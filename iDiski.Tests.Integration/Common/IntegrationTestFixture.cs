using System;
using System.Threading.Tasks;
using iDiski.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace iDiski.Tests.Integration.Common;

/// <summary>
/// A real PostgreSQL database, built by running the project's own migrations.
///
/// This used to be the EF InMemory provider, which was blind to the two classes of bug that
/// have actually broken production: it accepts DateTimes of any Kind, where Npgsql rejects
/// anything but UTC on a timestamptz column, and it builds the schema from the model via
/// EnsureCreated rather than replaying migrations, so a missing or malformed migration looked
/// perfectly healthy. It also enforces no unique index or check constraint, so tests could
/// write data the real database would refuse.
///
/// Each test class gets its own container, so classes cannot collide with one another.
/// Tests inside a class share one database, so they still need to use distinct values for
/// anything carrying a unique index — an email, a team short code, a division's season and
/// short code pair.
/// </summary>
public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public LeagueDbContext DbContext { get; private set; } = null!;

    /// <summary>Handy for anything that needs to reach the same database independently.</summary>
    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<LeagueDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            // Matching Program.cs: the API suppresses this so a deploy is never blocked by
            // model drift, and a fixture that did not would fail where production succeeds.
            .ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId
                    .PendingModelChangesWarning))
            .Options;

        DbContext = new LeagueDbContext(options);

        // Migrate, not EnsureCreated: the point is to exercise the migrations that will run
        // against the real database on deploy, rather than a schema inferred from the model.
        await DbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (DbContext is not null)
        {
            await DbContext.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }
}
