using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using UniShop.Api.Services;
using Xunit;

namespace UniShop.Tests;

public class AuthApiTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Register_creates_client_and_returns_token()
    {
        using var client = fixture.Factory.CreateClient();
        var request = NewRegisterRequest();

        using var response = await client.PostAsJsonAsync("/api/auth/register", request);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
        Assert.Contains("Client", auth.User.Roles);
        Assert.True(auth.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_returns_token_for_registered_client()
    {
        using var client = fixture.Factory.CreateClient();
        var request = NewRegisterRequest();
        await RegisterAsync(client, request);

        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            request.Email,
            request.Password
        });
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(auth);
        Assert.Equal(request.Email, auth.User.Email);
    }

    [Fact]
    public async Task Me_requires_token_and_returns_authenticated_user()
    {
        using var client = fixture.Factory.CreateClient();
        using var anonymousResponse = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        var request = NewRegisterRequest();
        var auth = await RegisterAsync(client, request);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        using var response = await client.GetAsync("/api/auth/me");
        var user = await response.Content.ReadFromJsonAsync<AuthUserResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(user);
        Assert.Equal(request.Email, user.Email);
        Assert.Contains("Client", user.Roles);
    }

    [Fact]
    public async Task Duplicate_email_returns_conflict()
    {
        using var client = fixture.Factory.CreateClient();
        var request = NewRegisterRequest();
        using var firstResponse = await client.PostAsJsonAsync("/api/auth/register", request);
        using var duplicateResponse = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task Wrong_email_and_wrong_password_return_same_unauthorized_message()
    {
        using var client = fixture.Factory.CreateClient();
        var request = NewRegisterRequest();
        await RegisterAsync(client, request);

        using var wrongPasswordResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            request.Email,
            Password = "IncorrectPassword1"
        });
        using var wrongEmailResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = $"missing-{Guid.NewGuid():N}@example.test",
            request.Password
        });
        var wrongPassword = await wrongPasswordResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        var wrongEmail = await wrongEmailResponse.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPasswordResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongEmailResponse.StatusCode);
        Assert.Equal(wrongPassword?.Detail, wrongEmail?.Detail);
    }

    [Fact]
    public async Task Client_token_is_forbidden_from_admin_endpoint()
    {
        using var client = fixture.Factory.CreateClient();
        var auth = await RegisterAsync(client, NewRegisterRequest());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        using var response = await client.GetAsync("/api/admin/access-check");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Seeded_admin_can_login_and_access_admin_endpoint()
    {
        using var client = fixture.Factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "admin@unishop.test",
            Password = "AdminPass123!"
        });
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(auth);
        Assert.Contains("Admin", auth.User.Roles);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        using var adminResponse = await client.GetAsync("/api/admin/access-check");
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
    }

    [Fact]
    public async Task Repeated_failed_logins_lock_out_account()
    {
        using var client = fixture.Factory.CreateClient();
        var request = NewRegisterRequest();
        await RegisterAsync(client, request);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failedResponse = await client.PostAsJsonAsync("/api/auth/login", new
            {
                request.Email,
                Password = "IncorrectPassword1"
            });
            Assert.Equal(HttpStatusCode.Unauthorized, failedResponse.StatusCode);
        }

        using var lockedResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            request.Email,
            request.Password
        });
        Assert.Equal(HttpStatusCode.Unauthorized, lockedResponse.StatusCode);
    }

    private static RegistrationRequest NewRegisterRequest() => new(
        "Test Client",
        $"client-{Guid.NewGuid():N}@example.test",
        "ClientPass123");

    private static async Task<AuthResponse> RegisterAsync(HttpClient client, object request)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private sealed record RegistrationRequest(string FullName, string Email, string Password);
}