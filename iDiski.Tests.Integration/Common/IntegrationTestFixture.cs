using System;
using System.Threading.Tasks;
using iDiski.Api;
using iDiski.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace iDiski.Tests.Integration.Common;

public class IntegrationTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private string? _dbPath;

    public HttpClient HttpClient { get; private set; } = null!;
    public LeagueDbContext DbContext { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"idiski_test_{Guid.NewGuid()}.db");

        builder.ConfigureServices(services =>
        {
            // Remove default DbContext
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<LeagueDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Use in-memory database for tests
            services.AddDbContext<LeagueDbContext>(options =>
                options.UseSqlite($"Data Source={_dbPath}"));
        });
    }

    public async Task InitializeAsync()
    {
        using (var scope = Services.CreateScope())
        {
            DbContext = scope.ServiceProvider.GetRequiredService<LeagueDbContext>();
            await DbContext.Database.EnsureCreatedAsync();
        }

        HttpClient = CreateClient();
    }

    public async Task DisposeAsync()
    {
        if (DbContext != null)
        {
            await DbContext.DisposeAsync();
        }

        if (_dbPath != null && File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }

        HttpClient.Dispose();
        await base.DisposeAsync();
    }
}
