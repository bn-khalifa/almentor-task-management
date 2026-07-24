.PHONY: help up down logs dev build test migrate migration seed clean

API_PROJECT := src/Almentor.TaskApi.Api
INFRA_PROJECT := src/Almentor.TaskApi.Infrastructure
SOLUTION := Almentor.TaskApi.slnx

help: ## Show this help
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  %-12s %s\n", $$1, $$2}'

up: ## Start the full stack (API + SQL Server) via Docker Compose
	docker compose up -d --build

down: ## Stop the Docker Compose stack (data persists in the named volume)
	docker compose down

logs: ## Follow the API container's logs
	docker compose logs -f api

dev: ## Run the API locally (dotnet run), against the Dockerized SQL Server
	dotnet run --project $(API_PROJECT)

build: ## Build the whole solution
	dotnet build $(SOLUTION)

test: ## Run the full test suite (unit + integration; Docker must be running)
	dotnet test tests/Almentor.TaskApi.Tests

migrate: ## Apply pending EF Core migrations to the database
	dotnet ef database update --project $(INFRA_PROJECT) --startup-project $(API_PROJECT)

migration: ## Create a new migration: make migration name=AddSomething
	dotnet ef migrations add $(name) --project $(INFRA_PROJECT) --startup-project $(API_PROJECT) --output-dir Persistence/Migrations

seed: ## Populate the database with sample data (auto-runs on startup if empty; this (re)starts the API to trigger it)
	docker compose up -d --build api

clean: ## Remove build artifacts (bin/obj) across the solution
	dotnet clean $(SOLUTION)
	find . -type d \( -name bin -o -name obj \) -not -path "*/node_modules/*" -exec rm -rf {} +
