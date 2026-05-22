// ┌─────────────────────────────────────────────────────────────────────────┐
// │  PATCH — add this to Infrastructure/Persistence/LeagueDbContext.cs      │
// │                                                                          │
// │  Replace the class declaration line:                                     │
// │    public class LeagueDbContext : DbContext                              │
// │  with:                                                                   │
// │    public class LeagueDbContext : DbContext, ILeagueDbContext            │
// │                                                                          │
// │  And add the using at the top:                                           │
// │    using Application.Common.Interfaces;                                  │
// │                                                                          │
// │  No other changes needed — DbContext already satisfies the interface     │
// │  because the DbSet<T> properties and SaveChangesAsync match exactly.     │
// └─────────────────────────────────────────────────────────────────────────┘

// ── Infrastructure DependencyInjection extension ──────────────────────────────
// Add this file as Infrastructure/DependencyInjection.cs

using iDiski.Application.Common.Interfaces;
using iDiski.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace iDiski.Infrastructure.Persistence;

public static class DependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure-layer services: EF Core DbContext + ILeagueDbContext.
    ///
    /// Call from Program.cs: builder.Services.AddInfrastructureServices(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<LeagueDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsAssembly("iDiski"); // replace with your startup project name
                    npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                }));

        // Bind the interface to the concrete DbContext so Application handlers
        // receive ILeagueDbContext via constructor injection — never LeagueDbContext directly.
        services.AddScoped<ILeagueDbContext>(provider =>
            provider.GetRequiredService<LeagueDbContext>());

        return services;
    }
}
