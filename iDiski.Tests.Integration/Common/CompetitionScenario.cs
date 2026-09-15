using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using iDiski.Application.Common.Interfaces;
using iDiski.Domain.Entities;

namespace iDiski.Tests.Integration.Common;

/// <summary>
/// Setting up a division, a competition and its entrants.
///
/// Almost every test needs the same three steps now — a pool of teams, something for them to
/// play, and an entry list saying which of them are in it — and writing those out per test
/// buried what each test was actually about.
/// </summary>
public static class CompetitionScenario
{
    /// <summary>A pool of teams. No format: a division is not a competition any more.</summary>
    public static async Task<Guid> ADivisionAsync(
        ILeagueDbContext db,
        Gender gender = Gender.Male,
        string name = "Division",
        int season = 2040)
    {
        var id = Guid.NewGuid();

        db.Divisions.Add(new Division
        {
            Id = id,
            Name = $"{name} {TestIds.Code("N")}",
            ShortCode = TestIds.Code("DV"),
            Season = season,
            Gender = gender,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(default);
        return id;
    }

    /// <summary>Something for them to play. Entrants are added separately, on purpose.</summary>
    public static async Task<Guid> ACompetitionAsync(
        ILeagueDbContext db,
        Guid divisionId,
        CompetitionFormat format = CompetitionFormat.League,
        int season = 2040,
        string name = "Competition")
    {
        var id = Guid.NewGuid();

        db.Competitions.Add(new Competition
        {
            Id = id,
            DivisionId = divisionId,
            Name = $"{name} {TestIds.Code("N")}",
            ShortCode = TestIds.Code("CP"),
            Season = season,
            Format = format,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(default);
        return id;
    }

    /// <summary>
    /// Clubs in a division, optionally entered into a competition. Splitting those two is the
    /// point of the whole model: a division of twenty can send eight of them to a cup.
    /// </summary>
    public static async Task<List<Guid>> ClubsAsync(
        ILeagueDbContext db,
        Guid divisionId,
        int count,
        Guid? enterInto = null)
    {
        var ids = new List<Guid>();

        for (var i = 0; i < count; i++)
        {
            var id = Guid.NewGuid();
            ids.Add(id);

            db.Teams.Add(new Team
            {
                Id = id,
                // Named in creation order so a test can predict who the generator draws first.
                Name = $"Club {i:D2} {TestIds.Code("N")}",
                ShortCode = TestIds.Code("T"),
                DivisionId = divisionId,
                Founded = 2020,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync(default);

        if (enterInto.HasValue) await EnterAsync(db, enterInto.Value, ids);

        return ids;
    }

    /// <summary>Puts clubs into a competition, whatever division they came from.</summary>
    public static async Task EnterAsync(
        ILeagueDbContext db,
        Guid competitionId,
        IEnumerable<Guid> teamIds)
    {
        foreach (var teamId in teamIds)
        {
            db.CompetitionEntries.Add(new CompetitionEntry
            {
                Id = Guid.NewGuid(),
                CompetitionId = competitionId,
                TeamId = teamId,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync(default);
    }

    /// <summary>The common case: a division, a competition, and everybody entered.</summary>
    public static async Task<(Guid DivisionId, Guid CompetitionId, List<Guid> TeamIds)> AWholeAsync(
        ILeagueDbContext db,
        CompetitionFormat format,
        int teams,
        Gender gender = Gender.Male,
        int season = 2040)
    {
        var divisionId = await ADivisionAsync(db, gender, season: season);
        var competitionId = await ACompetitionAsync(db, divisionId, format, season);
        var teamIds = await ClubsAsync(db, divisionId, teams, competitionId);

        return (divisionId, competitionId, teamIds);
    }
}
