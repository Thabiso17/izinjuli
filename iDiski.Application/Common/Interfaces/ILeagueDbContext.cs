using iDiski.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Application.Common.Interfaces;

/// <summary>
/// Abstracts EF Core away from the Application layer.
/// Application handlers depend only on this interface — never on LeagueDbContext directly.
/// </summary>
public interface ILeagueDbContext
{
    DbSet<Team>             Teams             { get; }
    DbSet<Player>           Players           { get; }
    DbSet<MatchResult>      MatchResults      { get; }
    DbSet<Article>          Articles          { get; }
    DbSet<Sponsor>          Sponsors          { get; }
    DbSet<PageLayoutConfig> PageLayoutConfigs { get; }
    DbSet<Division>         Divisions         { get; }
    DbSet<MatchEvent>       MatchEvents       { get; }
    DbSet<Suspension>       Suspensions       { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
