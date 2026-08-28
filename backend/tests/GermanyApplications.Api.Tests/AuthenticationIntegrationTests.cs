using System.Net;
using System.Net.Http.Json;
using GermanyApplications.Api.Authentication;
using GermanyApplications.Api.Data;
using GermanyApplications.Api.Domain.Entities;
using GermanyApplications.Api.Email;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GermanyApplications.Api.Tests;

public sealed class AuthenticationIntegrationTests : IClassFixture<AuthenticationIntegrationTests.AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public AuthenticationIntegrationTests(AuthApiFactory factory) => _factory = factory;

    [Fact]
    public async Task RegisterVerifyLoginAndCurrentUser_UsesSecureCookieSession()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var csrf = await GetCsrfTokenAsync(client);
        var email = $"student-{Guid.NewGuid():N}@example.test";

        using var register = await PostWithCsrfAsync(
            client, "/api/auth/register", new RegisterRequest(email, "Strong-Password-123!", "en"), csrf);
        Assert.Equal(HttpStatusCode.Accepted, register.StatusCode);

        await ConfirmEmailDirectlyAsync(email);
        csrf = await GetCsrfTokenAsync(client);
        using var login = await PostWithCsrfAsync(
            client, "/api/auth/login", new LoginRequest(email, "Strong-Password-123!"), csrf);
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), value => value.StartsWith("GermanyApplications.Session="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);

        var currentUser = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");
        Assert.NotNull(currentUser);
        Assert.Equal(email, currentUser.Email);
        Assert.Contains("Student", currentUser.Roles);
        Assert.DoesNotContain("PasswordHash", await client.GetStringAsync("/api/auth/me"));
    }

    [Fact]
    public async Task LoginFailureAndPasswordRecovery_DoNotRevealAccountExistence()
    {
        using var client = _factory.CreateClient();
        var csrf = await GetCsrfTokenAsync(client);
        using var login = await PostWithCsrfAsync(
            client, "/api/auth/login", new LoginRequest("missing@example.test", "Not-The-Password-1!"), csrf);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Contains("Unable to sign in", await login.Content.ReadAsStringAsync());

        csrf = await GetCsrfTokenAsync(client);
        using var recovery = await PostWithCsrfAsync(
            client, "/api/auth/forgot-password", new ForgotPasswordRequest("missing@example.test"), csrf);
        Assert.Equal(HttpStatusCode.Accepted, recovery.StatusCode);
        Assert.Contains("If the account exists", await recovery.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task StateChangingAuthenticationEndpoint_RejectsMissingCsrfToken()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("student@example.test", "Strong-Password-123!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task ConfirmEmailDirectlyAsync(string email)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByEmailAsync(email) ?? throw new InvalidOperationException("Test user not found.");
        var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        Assert.True((await userManager.ConfirmEmailAsync(user, code)).Succeeded);
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<CsrfTokenResponse>("/api/auth/csrf");
        return response?.Token ?? throw new InvalidOperationException("CSRF token missing.");
    }

    private static async Task<HttpResponseMessage> PostWithCsrfAsync<T>(
        HttpClient client,
        string path,
        T payload,
        string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("X-CSRF-TOKEN", token);
        return await client.SendAsync(request);
    }

    public sealed class AuthApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private const string TestConnection = "Host=localhost;Port=5433;Database=germany_applications_tests;Username=germany_app_test;Password=local-test-only";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = TestConnection,
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173"
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<ApplicationDbContext>();
                services.RemoveAll<IAccountEmailSender>();
                services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(TestConnection));
                services.AddSingleton<IAccountEmailSender>(new CapturingEmailSender());
            });
        }

        public async Task InitializeAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await database.Database.EnsureDeletedAsync();
            await database.Database.MigrateAsync();
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await database.Database.EnsureDeletedAsync();
            await base.DisposeAsync();
        }
    }

    private sealed class CapturingEmailSender : IAccountEmailSender
    {
        public Task SendEmailVerificationAsync(Guid userId, string email, string encodedCode, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SendPasswordResetAsync(Guid userId, string email, string encodedCode, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
