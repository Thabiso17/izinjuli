using iDiski.Domain.Entities;
using iDiski.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Infrastructure.Seed;

/// <summary>
/// Gives a seeded division the league it is running.
///
/// Every seeder in this folder predates competitions: they create a division, fill it with
/// clubs, and write its fixtures, because a division used to be the competition. A division
/// with fixtures and no competition is now a division with nothing to show — no table, no
/// status, nothing on its page.
///
/// Rather than thread competitions through every seeding method, each seeder calls this once
/// at the end. It does to a freshly seeded database exactly what the migration does to a live
/// one, so an environment seeded from scratch and an upgraded one look the same.
/// </summary>
public static class CompetitionBackfill
{
    /// <summary>
    /// Guarded: a division already running something is left alone, so calling this twice —
    /// or calling it from two seeders that touched the same division — changes nothing.
    /// </summary>
    public static async Task GiveEveryDivisionItsCompetitionAsync(LeagueDbContext context)
    {
        var divisions = await context.Divisions
            .Include(d => d.Teams)
            .ToListAsync();

        var alreadyRunning = await context.Competitions
            .Select(c => c.DivisionId)
            .ToListAsync();

        var added = 0;

        foreach (var division in divisions.Where(d => !alreadyRunning.Contains(d.Id)))
        {
            var competition = new Competition
            {
                Id = Guid.NewGuid(),
                DivisionId = division.Id,
                Name = division.Name,
                // Short codes are unique per division and season, and there is exactly one
                // competition per division here, so the division's own code cannot clash.
                ShortCode = division.ShortCode,
                Season = division.Season,
                Format = CompetitionFormat.League,
                StartDate = division.StartDate,
                EndDate = division.EndDate,
                IsActive = division.IsActive,
                CreatedAt = DateTime.UtcNow,
            };

            context.Competitions.Add(competition);

            foreach (var team in division.Teams)
            {
                context.CompetitionEntries.Add(new CompetitionEntry
                {
                    Id = Guid.NewGuid(),
                    CompetitionId = competition.Id,
                    TeamId = team.Id,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            var fixtures = await context.MatchResults
                .Where(m => m.DivisionId == division.Id && m.CompetitionId == null)
                .ToListAsync();

            foreach (var fixture in fixtures)
                fixture.CompetitionId = competition.Id;

            added++;
        }

        if (added > 0)
        {
            await context.SaveChangesAsync();
            Console.WriteLine($"✅ Gave {added} divisions their league as a competition");
        }
    }
}
