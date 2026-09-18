using iDiski.Domain.Entities;
using iDiski.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Infrastructure.Seed;

/// <summary>
/// Gives a seeded division's clubs a league to play in.
///
/// Every seeder in this folder predates competitions: they create a division, fill it with
/// clubs, and write its fixtures, because a division used to be the competition. A fixture
/// with no competition now belongs to nothing — no table, no status, nothing on any page.
///
/// So each seeder calls this once at the end and every division's clubs end up entered in a
/// league of their own. That is a convenience of seeding, not a rule: nothing afterwards
/// records that the competition came from a division, because nothing owns one.
/// </summary>
public static class CompetitionBackfill
{
    /// <summary>
    /// Guarded: a division whose clubs are already entered in something is left alone, so
    /// calling this twice — or from two seeders that touched the same division — changes
    /// nothing.
    /// </summary>
    public static async Task GiveEveryDivisionItsCompetitionAsync(LeagueDbContext context)
    {
        var divisions = await context.Divisions
            .Include(d => d.Teams)
            .ToListAsync();

        // Short codes are unique per season now that nothing scopes them any narrower, so a
        // division's own code can collide with another division's in the same year.
        var taken = await context.Competitions
            .Select(c => new { c.Season, c.ShortCode })
            .ToListAsync();

        var used = taken.Select(t => $"{t.Season}:{t.ShortCode}").ToHashSet();

        var entered = await context.CompetitionEntries
            .Select(e => e.TeamId)
            .ToListAsync();

        var alreadyPlaying = entered.ToHashSet();
        var added = 0;

        foreach (var division in divisions)
        {
            // Nothing to play and nobody to play it, or its clubs are already in something.
            if (division.Teams.Count == 0) continue;
            if (division.Teams.Any(t => alreadyPlaying.Contains(t.Id))) continue;

            var shortCode = UnusedCode(division.ShortCode, division.Season, used);
            used.Add($"{division.Season}:{shortCode}");

            var competition = new Competition
            {
                Id = Guid.NewGuid(),
                Name = division.Name,
                ShortCode = shortCode,
                Season = division.Season,
                Format = CompetitionFormat.League,
                Gender = division.Gender ?? Gender.Male,
                AgeGroup = division.AgeGroup,
                StartDate = division.StartDate,
                EndDate = division.EndDate,
                IsActive = division.IsActive,
                CreatedAt = DateTime.UtcNow,
            };

            context.Competitions.Add(competition);

            var teamIds = division.Teams.Select(t => t.Id).ToList();

            foreach (var teamId in teamIds)
            {
                context.CompetitionEntries.Add(new CompetitionEntry
                {
                    Id = Guid.NewGuid(),
                    CompetitionId = competition.Id,
                    TeamId = teamId,
                    CreatedAt = DateTime.UtcNow,
                });

                alreadyPlaying.Add(teamId);
            }

            // The fixtures these clubs already have, which were written before there was
            // anything for them to belong to.
            var fixtures = await context.MatchResults
                .Where(m => m.CompetitionId == null
                            && m.HomeTeamId != null
                            && teamIds.Contains(m.HomeTeamId.Value))
                .ToListAsync();

            foreach (var fixture in fixtures)
                fixture.CompetitionId = competition.Id;

            added++;
        }

        if (added > 0)
        {
            await context.SaveChangesAsync();
            Console.WriteLine($"✅ Gave {added} divisions' clubs a league to play in");
        }
    }

    /// <summary>
    /// The division's own short code where it is free for that season, and a numbered variant
    /// where another division has already taken it.
    /// </summary>
    private static string UnusedCode(string preferred, int season, HashSet<string> used)
    {
        if (!used.Contains($"{season}:{preferred}")) return preferred;

        for (var n = 2; n < 100; n++)
        {
            var candidate = $"{preferred[..Math.Min(preferred.Length, 17)]}-{n}";
            if (!used.Contains($"{season}:{candidate}")) return candidate;
        }

        return $"{preferred[..Math.Min(preferred.Length, 12)]}-{Guid.NewGuid():N}"[..20];
    }
}
