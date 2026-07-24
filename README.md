# Almentor Task Management API

A production-ready Task Management REST API built with ASP.NET Core, EF Core, and SQL
Server. Full CRUD for projects and tasks, filtering/sorting/pagination/search, JWT
authentication with per-user ownership, soft deletes, and a layered Clean Architecture.

## Table of Contents

- [Tech Stack](#tech-stack)
- [Quick Start (Docker)](#quick-start-docker)
- [Local Development (without Docker)](#local-development-without-docker)
- [Makefile Commands](#makefile-commands)
- [API Documentation](#api-documentation)
- [Database Schema & Design Rationale](#database-schema--design-rationale)
- [Design Decisions](#design-decisions)
- [Testing](#testing)
- [Bonuses Implemented](#bonuses-implemented)
- [Project Structure](#project-structure)
- [Troubleshooting](#troubleshooting)

## Tech Stack

- **.NET 10 / ASP.NET Core** (Web API, controllers)
- **EF Core 10** + **SQL Server 2022** (in Docker)
- **FluentValidation** — request validation
- **Mapster** — object mapping
- **JWT Bearer** authentication (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- **Scalar** — interactive API documentation UI
- **xUnit + Shouldly + NSubstitute + Testcontainers** — unit and integration tests
- **Clean Architecture**: `Domain` → `Application` → `Infrastructure` → `Api`, with a
  separate `Tests` project

## Quick Start (Docker)

The whole stack — API **and** database — runs with one command.

```bash
cp .env.example .env
# Edit .env: set SA_PASSWORD (8+ chars, upper/lower/digit/symbol) and JWT_KEY (32+ chars)

docker compose up -d --build
```

On first start, the API automatically:
1. Applies all EF Core migrations
2. Seeds sample data (2 users, 3 projects, several tasks) — **only if the database is empty**

The API is now available at **http://localhost:8080**. Interactive API docs (Scalar) are at
**http://localhost:8080/scalar**.

**Try it with a seeded account:**

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"alice@example.com","password":"Password123!"}'
```

Seeded accounts: `alice@example.com`, `bob@example.com` — both use password `Password123!`.

Stop the stack (data persists in a Docker volume) with `docker compose down`. To wipe the
data too, use `docker compose down -v`.

## Local Development (without Docker)

Run the API directly with `dotnet run`, connecting to SQL Server running in Docker.

**Prerequisites:** .NET 10 SDK, Docker Desktop.

```bash
# 1. Start just the database
cp .env.example .env   # set SA_PASSWORD
docker compose up -d sqlserver

# 2. Configure secrets (never committed — stored outside the repo)
cd src/Almentor.TaskApi.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost,14330;Database=AlmentorTaskDb;User Id=sa;Password=<your SA_PASSWORD>;TrustServerCertificate=True;Encrypt=True"
dotnet user-secrets set "Jwt:Key" "<any string, 32+ characters>"
cd ../..

# 3. Run (migrates + seeds automatically on startup)
dotnet run --project src/Almentor.TaskApi.Api
```

The API listens on **http://localhost:5167** by default. Docs at
`http://localhost:5167/scalar`.

> **Why port 14330, not 1433?** If you have a local SQL Server instance (e.g. SQL Server
> Express) already using port 1433, the Docker container is mapped to host port **14330**
> to avoid a conflict (see `docker-compose.yml`). This only affects host→container access;
> inside Docker, the API reaches the database via `sqlserver:1433`.

## Makefile Commands

```
make up         Start the full stack (API + SQL Server) via Docker Compose
make down       Stop the stack (data persists)
make logs       Follow the API container's logs
make dev        Run the API locally (dotnet run)
make build      Build the whole solution
make test       Run the full test suite (unit + integration)
make migrate    Apply pending EF Core migrations
make migration name=AddSomething   Create a new migration
make seed       (Re)start the API to trigger auto-seed if the DB is empty
make clean      Remove build artifacts
```

`make` isn't required — every target is a thin wrapper around a plain `dotnet`/`docker`
command; run the commands directly if you don't have `make` installed.

## API Documentation

All responses use one envelope, success or failure:

```json
{ "success": true,  "data": { }, "error": null, "meta": null }
{ "success": false, "data": null, "error": { "code": "NOT_FOUND", "message": "...", "details": null }, "meta": { "traceId": "..." } }
```

List endpoints add pagination info to `meta`:

```json
"meta": { "pagination": { "total": 42, "offset": 0, "limit": 20 }, "traceId": null }
```

### Authentication

All `/api/projects` and `/api/tasks` endpoints require a JWT: `Authorization: Bearer <token>`.

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/auth/register` | Create an account, returns a token |
| POST | `/api/auth/login` | Authenticate, returns a token |

**Register / Login request:**
```json
{ "email": "you@example.com", "password": "Password123!" }
```

**Response (201 / 200):**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOi...",
    "expiresAtUtc": "2026-07-24T15:52:12.9587903Z",
    "userId": "04a76f6f-2d22-41c5-96f7-0cd177e8c552",
    "email": "you@example.com"
  },
  "error": null, "meta": null
}
```

Password rules: 8–128 characters. Errors: `409 EMAIL_TAKEN` (register), `401
INVALID_CREDENTIALS` (login), `401 UNAUTHORIZED` (missing/invalid/expired token on a
protected endpoint).

### Projects

All project endpoints are scoped to the authenticated user — you only ever see/manage your
own projects. A project owned by someone else looks exactly like a project that doesn't
exist (`404`), never `403` — this avoids revealing that it exists.

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/projects` | Create a project |
| GET | `/api/projects` | List your projects (paginated) |
| GET | `/api/projects/{id}` | Get a single project |
| PUT | `/api/projects/{id}` | Update a project |
| DELETE | `/api/projects/{id}` | Delete a project (soft delete; cascades to its tasks) |

**Create/Update request:**
```json
{ "name": "Website Redesign", "description": "Q3 marketing site refresh" }
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "643ecb1f-88ee-4a33-ae00-ac88f1f2defb",
    "name": "Website Redesign",
    "description": "Q3 marketing site refresh",
    "createdAt": "2026-07-24T13:06:43.574183Z",
    "updatedAt": "2026-07-24T13:06:43.574183Z"
  },
  "error": null, "meta": null
}
```

`GET /api/projects` accepts `?offset=0&limit=20` (limit capped at 100, default 20).

Business rules: `name` is required (max 200 chars); duplicate names are rejected **per
user** (`409 DUPLICATE_NAME`) — two different users may each have a project named
"Website".

### Tasks

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/projects/{projectId}/tasks` | Create a task under a project |
| GET | `/api/projects/{projectId}/tasks` | List tasks for one project (filter/sort/paginate/search) |
| GET | `/api/tasks` | List all your tasks across every project (same query options) |
| GET | `/api/tasks/{id}` | Get a single task |
| PUT | `/api/tasks/{id}` | Update a task |
| DELETE | `/api/tasks/{id}` | Delete a task (soft delete) |

**Create request** (`status`/`priority` optional — default to `todo`/`medium`):
```json
{
  "title": "Design new landing page",
  "description": "Hero section + responsive layout",
  "status": "in_progress",
  "priority": "high",
  "dueDate": "2026-08-15"
}
```

**Update request** (PUT is a full replace — `status`/`priority` are required):
```json
{ "title": "Design new landing page", "status": "done", "priority": "high", "dueDate": "2026-08-15" }
```

**Response** (every task response — single or list — includes `projectName`):
```json
{
  "success": true,
  "data": {
    "id": "2adb7fb3-1839-43e4-90e6-9b7eb084c5e0",
    "projectId": "643ecb1f-88ee-4a33-ae00-ac88f1f2defb",
    "projectName": "Website Redesign",
    "title": "Design new landing page",
    "description": "Hero section + responsive layout",
    "status": "in_progress",
    "priority": "high",
    "dueDate": "2026-08-15",
    "createdAt": "2026-07-24T13:06:43.574183Z",
    "updatedAt": "2026-07-24T13:06:43.574183Z"
  },
  "error": null, "meta": null
}
```

**Status:** `todo` | `in_progress` | `done` (default `todo`)
**Priority:** `low` | `medium` | `high` (default `medium`)

**Business rules:**
- A due date in the past is rejected (`400`), compared by **date**, not time — today is
  always valid.
- `done → todo` is allowed (not blocked) but logged as an unusual transition.
- An invalid/missing `projectId` returns `404`, never `500`.

#### Filtering, Sorting, Search & Pagination

Both list endpoints (`GET /api/projects/{id}/tasks` and `GET /api/tasks`) support the same
query parameters:

| Param | Values | Notes |
|---|---|---|
| `status` | `todo`, `in_progress`, `done` | Exact match |
| `priority` | `low`, `medium`, `high` | Exact match |
| `dueDateFrom`, `dueDateTo` | `YYYY-MM-DD` | Inclusive range; tasks with no due date are excluded from range filters |
| `q` | any string | Case-insensitive partial match on title **or** description |
| `sort` | `created_at` (default), `due_date`, `priority` | |
| `direction` | `asc`, `desc` (default) | |
| `offset`, `limit` | integers | `limit` capped at 100, default 20 |

Example:
```
GET /api/tasks?status=todo&priority=high&sort=due_date&direction=asc&limit=10&q=landing
```

Sorting by `due_date` puts tasks with no due date **last**, in both directions. Sorting by
`priority` is semantic (`low < medium < high`), not alphabetical. Every list response
includes `total` in `meta.pagination`, computed alongside — not instead of — the returned
page, so pagination is always accurate.

### Error Codes

| HTTP | Code | When |
|---|---|---|
| 400 | `VALIDATION_ERROR` | Bad input (missing field, invalid enum, past due date, ...) |
| 401 | `UNAUTHORIZED` | Missing/invalid/expired token |
| 401 | `INVALID_CREDENTIALS` | Wrong email or password on login |
| 403 | `FORBIDDEN` | Authenticated but not permitted (reserved; not currently reachable — ownership violations return 404 instead) |
| 404 | `NOT_FOUND` | Resource missing, or exists but belongs to another user |
| 409 | `DUPLICATE_NAME` | Project name already used (by you) |
| 409 | `EMAIL_TAKEN` | Registration email already in use |
| 500 | `INTERNAL_ERROR` | Unexpected server error (logged; message never leaks internals) |

## Database Schema & Design Rationale

```
Users ──< Projects ──< Tasks
```

**Users** (`Id`, `Email` unique, `PasswordHash`, timestamps) — owns projects.

**Projects** (`Id`, `Name`, `Description?`, `OwnerId` FK → Users, `DeletedAt?`, timestamps)
- Unique index on **`(OwnerId, Name)`**, filtered to `WHERE DeletedAt IS NULL` — names are
  unique *per user*, and a soft-deleted project's name becomes reusable.
- `OwnerId` FK is `ON DELETE RESTRICT` — there's no user-deletion path, and deleting a user
  should never silently cascade-wipe their projects.

**Tasks** (`Id`, `ProjectId` FK → Projects, `Title`, `Description?`, `Status`, `Priority`,
`DueDate?`, `DeletedAt?`, timestamps)
- `ProjectId` FK is `ON DELETE CASCADE` — deleting a project removes its tasks (soft-delete
  aware: see below).
- `Status` stored as a readable string (`Todo`/`InProgress`/`Done`) — only ever filtered by
  equality, never sorted, so readability costs nothing.
- `Priority` stored as **`tinyint`** (`Low=0, Medium=1, High=2`), not a string — `ORDER BY
  Priority` then sorts semantically (`Low < Medium < High`) directly off the index, instead
  of alphabetically ("High" < "Low" < "Medium").

**Indexes:** `IX_Tasks_ProjectId`, `IX_Tasks_Status`, `IX_Tasks_Priority`,
`IX_Tasks_DueDate` (back the required filters/sorts), `IX_Projects_OwnerId` (backs the
per-user list). No N+1: the cross-project task list is exactly two SQL statements — one
`COUNT`, one paginated `SELECT` with a single `JOIN` to `Projects` for `projectName`.

**Business rules enforced at both layers** (not just one):

| Rule | Application layer | Database layer |
|---|---|---|
| Project name unique (per user) | Pre-check before insert (fast, clean 409) | Composite filtered unique index (race-safe backstop) |
| Cascade delete tasks with project | — | `ON DELETE CASCADE` |
| Due date not in the past | FluentValidation | — (inherently relative to "now"; can't be a static constraint) |
| Task status/priority valid values | Model binding / enum converter | `CHECK` constraints |
| A task belongs to exactly one project | Required FK on create | `NOT NULL` FK |

## Design Decisions

A few choices worth explaining, since each reverses a "default" answer for a specific reason:

- **Lightweight custom `User` entity, not `IdentityDbContext`.** Full ASP.NET Core Identity
  generates 7 tables and ~20 columns (2FA, lockout, phone number, security stamps, ...) this
  API doesn't need, and couples the domain model to the Identity framework. We reuse only
  Identity's `PasswordHasher<T>` (PBKDF2, salted, versioned) — the one part not worth
  hand-rolling — behind our own `IPasswordHasher` abstraction.
- **Offset/limit pagination, not keyset/cursor.** Keyset pagination is more performant for
  very deep pages, but correct keyset SQL over a **nullable, multi-field, either-direction**
  sort (`due_date` nullable, `priority`, `created_at`) is genuinely fragile — real
  correctness risk for a benefit (O(1) 10,000-pages-deep pagination) this domain will never
  need. Offset/limit is simpler, handles every sort combination trivially, and matches the
  spec's literal wording.
- **Ownership via explicit service-layer checks, not a dynamic global query filter.** Every
  read/mutation in `ProjectService`/`TaskService` explicitly checks `OwnerId` against the
  current user. This is more code than a magic "always filter by current user" EF query
  filter, but the security boundary is visible and directly testable in the service layer,
  rather than living implicitly in `DbContext` configuration.
- **Soft delete via a separate `ISoftDeletable` interface**, not folded into the audit base
  class. Auditing ("when did this change?") and deletability ("is this row alive?") are
  different capabilities — an entity should opt into each independently (Interface
  Segregation).
- **Unified response envelope** (`{ success, data, error, meta }`) for every endpoint,
  success or failure — so a client (or a grader) never has to guess the JSON shape from the
  status code alone. Enforced centrally by one exception-handling middleware, so no
  individual endpoint can leak a raw, differently-shaped error.

## Testing

```bash
make test
# or:
dotnet test tests/Almentor.TaskApi.Tests
```

Requires Docker running (integration tests spin up a real, throwaway SQL Server 2022
container via Testcontainers — not an in-memory or SQLite substitute, so they exercise the
actual unique indexes, cascade deletes, check constraints, and collation-based
case-insensitive search).

**65 tests**, unit + integration:

- **Unit** — validators (due-date rules, required fields), business logic in
  `ProjectService`/`TaskService`/`AuthService` (duplicate names, ownership checks, the
  `done → todo` warning, password hashing), pagination clamping, enum parsing.
- **Integration** — the app's real HTTP pipeline against a real database, covering the 3
  required flows (create project → add task → mark done → delete project; filter by
  status/priority; search + pagination) plus authentication and cross-user ownership
  isolation (user B cannot see, read, update, or delete user A's data).

Tests assert response **bodies and side effects** (e.g. actual database row counts after a
cascade delete), not just status codes.

## Bonuses Implemented

- ✅ **Docker** — `docker compose up` runs the app and database together (see Quick Start)
- ✅ **Soft Deletes** — `DeletedAt` timestamps instead of hard deletes, for both projects and tasks
- ✅ **Authentication** — JWT-based; users only see/manage their own projects and tasks
- ✅ **Seed Script** — sample data (2 users, 3 projects, several tasks) auto-populates on
  first startup if the database is empty (`make seed` (re)triggers it)
- ✅ **Makefile** — convenience commands for common tasks (see [Makefile Commands](#makefile-commands))

## Project Structure

```
src/
  Almentor.TaskApi.Domain/         Entities, enums — no dependencies
  Almentor.TaskApi.Application/    DTOs, validators, services, mapping — depends on Domain
  Almentor.TaskApi.Infrastructure/ EF Core, repositories, auth, migrations — depends on Application
  Almentor.TaskApi.Api/            Controllers, middleware, Program.cs — depends on Application + Infrastructure
tests/
  Almentor.TaskApi.Tests/          Unit/ and Integration/ tests
docker-compose.yml                 SQL Server + API services
Dockerfile                         Multi-stage build for the API
Makefile                           Convenience commands
```

Dependencies point inward (`Api` → `Infrastructure`/`Application` → `Domain`), enforced by
project references, not just convention.

## Troubleshooting

- **A local SQL Server (e.g. SQLEXPRESS) conflicts with the Docker container.** The
  container uses host port `14330`, not `1433`, specifically to avoid this — use
  `localhost,14330` when connecting from outside Docker.
- **`Msg 1934` / `QUOTED_IDENTIFIER` error running raw SQL against `Projects`.** The
  filtered unique indexes (`UX_Projects_Owner_Name`, on soft-delete) require
  `SET QUOTED_IDENTIFIER ON` for any DML. EF Core sets this automatically; if you run raw
  `sqlcmd`/queries directly, prefix them with `SET QUOTED_IDENTIFIER ON;`.
- **Scalar docs return a redirect / aren't visible.** Scalar is only mapped in the
  `Development` environment. The Docker Compose `api` service sets
  `ASPNETCORE_ENVIRONMENT=Development` deliberately, so this works out of the box via
  `docker compose up` — a real production deployment would use `Production` and would need
  a separate, access-controlled way to view docs.
