using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using iDiski.Application.Authentication.Commands;
using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
using iDiski.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace iDiski.Tests.Integration.Common;

/// <summary>
/// The whole application, booted from its own Program.cs, answering real HTTP requests against
/// a real PostgreSQL database.
///
/// The other fixture drives handlers directly, which leaves a wide gap: routing, model binding,
/// the authorization policies and the ProblemDetails shape the Angular client reads all live in
/// the pipeline, not in the handlers. A handler test cannot tell you that an endpoint is
/// reachable without a token, or that the route template does not match the one the client
/// calls. These can.
///
/// Configuration is handed over as environment variables rather than through the factory's
/// builder hooks, because Program.cs reads Jwt and DATABASE_URL off the configuration before
/// it calls Build(), which is earlier than any callback a test registers would run.
/// </summary>
public class ApiTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    /// <summary>The password every user seeded by <see cref="SeedUserAsync"/> logs in with.</summary>
    public const string Password = "Str0ng-Test-Passw0rd!";

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Program.cs reads DATABASE_URL first and falls back to appsettings, so pointing it at
        // the container is all it takes for the app to migrate and run against this database.
        Environment.SetEnvironmentVariable("DATABASE_URL", _postgres.GetConnectionString());

        // Development keeps file storage local; anything else reaches for Cloudinary credentials
        // that do not exist here.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        Environment.SetEnvironmentVariable(
            "Jwt__SecretKey",
            "integration-tests-only-signing-key-long-enough-to-satisfy-the-256-bit-minimum");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "https://tests.izinjuli.local");
        Environment.SetEnvironmentVariable("Jwt__Audience", "https://tests.izinjuli.local");
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", "15");

        _factory = new WebApplicationFactory<Program>();

        // Building the client is what actually starts the host, and the host is what runs the
        // migrations. Do it here so a failure surfaces as a fixture failure rather than as an
        // unrelated-looking failure in whichever test ran first.
        using var warmUp = _factory.CreateClient();
    }

    public Task DisposeAsync() => DisposeCoreAsync();

    private async Task DisposeCoreAsync()
    {
        _factory?.Dispose();
        await _postgres.DisposeAsync();
    }

    /// <summary>An unauthenticated client, for checking what the public can reach.</summary>
    public HttpClient CreateClient() => _factory.CreateClient();

    /// <summary>A client carrying a token obtained through the real login endpoint.</summary>
    public async Task<HttpClient> CreateClientAsAsync(User user)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/authentication/login", new LoginCommand(user.Email, Password));
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>()
            ?? throw new InvalidOperationException("Login returned no body.");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);

        return client;
    }

    /// <summary>
    /// Runs work against the application's own database, for seeding a test's starting state
    /// and for checking afterwards what actually landed in the tables.
    /// </summary>
    public async Task WithDbAsync(Func<LeagueDbContext, Task> work)
    {
        using var scope = _factory.Services.CreateScope();
        await work(scope.ServiceProvider.GetRequiredService<LeagueDbContext>());
    }

    public async Task<T> WithDbAsync<T>(Func<LeagueDbContext, Task<T>> work)
    {
        using var scope = _factory.Services.CreateScope();
        return await work(scope.ServiceProvider.GetRequiredService<LeagueDbContext>());
    }

    /// <summary>
    /// Creates a user who can genuinely log in: the password is hashed with the application's
    /// own hasher, so the login endpoint verifies it the same way it would in production.
    /// </summary>
    public async Task<User> SeedUserAsync(Role role, Guid? teamId = null, Guid? divisionId = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LeagueDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var user = new User
        {
            Id = id,
            Email = $"{role.ToString().ToLowerInvariant()}-{id:N}@test.com",
            PasswordHash = hasher.HashPassword(Password),
            FirstName = role.ToString(),
            LastName = "Tester",
            IsActive = true,
            CreatedAt = now
        };

        db.Users.Add(user);
        db.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(), UserId = id, Role = role, AssignedAt = now, CreatedAt = now
        });

        if (teamId is not null)
        {
            db.UserTeams.Add(new UserTeam
            {
                Id = Guid.NewGuid(), UserId = id, TeamId = teamId.Value,
                AssignedAt = now, CreatedAt = now
            });
        }

        if (divisionId is not null)
        {
            db.UserDivisions.Add(new UserDivision
            {
                Id = Guid.NewGuid(), UserId = id, DivisionId = divisionId.Value,
                AssignedAt = now, CreatedAt = now
            });
        }

        await db.SaveChangesAsync();
        return user;
    }

    /// <summary>Short codes carry unique indexes, so every test needs its own.</summary>
    public static string Code(string prefix) =>
        $"{prefix}{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
}
