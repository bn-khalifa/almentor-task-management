using System.Net;
using System.Net.Http.Json;
using Almentor.TaskApi.Application.Common.Models;
using Almentor.TaskApi.Application.Features.Auth.Dtos;
using Almentor.TaskApi.Tests.Integration.Infrastructure;
using Shouldly;

namespace Almentor.TaskApi.Tests.Integration.Flows;

/// <summary>Registration/login and that protected endpoints actually require a token.</summary>
public class AuthFlowTests : IntegrationTestBase
{
    public AuthFlowTests(SqlServerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Register_then_login_issues_tokens_and_protected_call_succeeds()
    {
        // The base class already registered+authenticated the default Client;
        // a protected call with its token works.
        var response = await Client.GetAsync("/api/projects");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Calling_a_protected_endpoint_without_a_token_returns_401_in_the_envelope()
    {
        // A client against the same in-memory server, but with no Authorization header.
        using var anon = CreateUnauthenticatedClient();

        var response = await anon.GetAsync("/api/projects");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(TestJson.Options);
        body!.Success.ShouldBeFalse();
        body.Error!.Code.ShouldBe("UNAUTHORIZED");
    }

    [Fact]
    public async Task Registering_a_duplicate_email_returns_409()
    {
        var email = $"dupe_{Guid.NewGuid():N}@test.local";
        var first = await Client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Password123!" });
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        var second = await Client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Password123!" });
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var body = await second.Content.ReadFromJsonAsync<ApiResponse<object>>(TestJson.Options);
        body!.Error!.Code.ShouldBe("EMAIL_TAKEN");
    }

    [Fact]
    public async Task Logging_in_with_a_wrong_password_returns_401()
    {
        var email = $"login_{Guid.NewGuid():N}@test.local";
        await Client.PostAsJsonAsync("/api/auth/register", new { email, password = "Password123!" });

        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "WrongPassword1" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(TestJson.Options);
        body!.Error!.Code.ShouldBe("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Registering_with_a_short_password_returns_400()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register",
            new { email = "shortpw@test.local", password = "short" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
