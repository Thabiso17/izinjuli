using System;
using System.IO;
using System.Threading.Tasks;
using iDiski.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace iDiski.Tests.Integration.Common;

/// <summary>
/// Integration test fixture providing database context for end-to-end flow testing.
/// Uses in-memory SQLite database that is created fresh for each test.
/// </summary>
public class IntegrationTestFixture : IAsyncLifetime
{
    public LeagueDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<LeagueDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        DbContext = new LeagueDbContext(options);
        await DbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (DbContext != null)
        {
            await DbContext.DisposeAsync();
        }
    }
}
