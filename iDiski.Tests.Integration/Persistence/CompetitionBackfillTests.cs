using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
using iDiski.Infrastructure.Seed;
using iDiski.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace iDiski.Tests.Integration.Persistence;

/// <summary>
/// Giving a division that predates competitions the league it was already running.
///
/// This is the same work the AddCompetitions migration does to a live database, written twice
/// on purpose: the migration cannot run against a seeded environment and the seeders cannot
/// run against production. What is pinned here is the shared backfill the seeders call, which
/// is the half that can be tested — and the half the browser suite depends on, since it seeds
/// through <c>/api/seed</c> and a division arriving with fixtures but no competition would
/// leave every public page with nothing to show.
/// </summary>
public class CompetitionBackfillTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public CompetitionBackfillTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ADivisionFromBeforeCompetitionsGetsItsLeague()
    {
        var (divisionId, clubs) = await ADivisionOfTheOldShapeAsync(4);

        await CompetitionBackfill.GiveEveryDivisionItsCompetitionAsync(_fixture.DbContext);

        var competition = await _fixture.DbContext.Competitions
            .AsNoTracking()
            .SingleAsync(c => c.Entries.Any(e => e.Team.DivisionId == divisionId));

        // It carries the division's own details, so nothing reads differently afterwards.
        competition.Season.Should().Be(2039);
        competition.Format.Should().Be(CompetitionFormat.League);
        competition.Gender.Should().Be(Gender.Male, "taken from the division it came from");

        var entered = await _fixture.DbContext.CompetitionEntries
            .AsNoTracking()
            .Where(e => e.CompetitionId == competition.Id)
            .Select(e => e.TeamId)
            .ToListAsync();

        entered.Should().BeEquivalentTo(clubs, "every club in the division was playing in it");
    }

    [Fact]
    public async Task ItsFixturesArePointedAtTheNewCompetition()
    {
        // The one that matters most: a fixture left with no competition shows up nowhere —
        // not in the table, not on the division page, not on a club's list.
        var (divisionId, clubs) = await ADivisionOfTheOldShapeAsync(2);
        var fixtureId = await AnOldFixtureAsync(divisionId, clubs[0], clubs[1]);

        await CompetitionBackfill.GiveEveryDivisionItsCompetitionAsync(_fixture.DbContext);

        var competition = await _fixture.DbContext.Competitions
            .AsNoTracking()
            .SingleAsync(c => c.Entries.Any(e => e.Team.DivisionId == divisionId));

        var fixture = await _fixture.DbContext.MatchResults
            .AsNoTracking()
            .FirstAsync(m => m.Id == fixtureId);

        fixture.CompetitionId.Should().Be(competition.Id);
    }

    [Fact]
    public async Task ADivisionAlreadyRunningSomethingIsLeftAlone()
    {
        // Guarded, because the seeders call it one after another and a second pass must not
        // hand the same clubs a second league. The guard is whether they are already playing
        // in something, since nothing else now ties a competition to where its clubs came from.
        var divisionId = await CompetitionScenario.ADivisionAsync(_fixture.DbContext, season: 2039);
        var existing = await CompetitionScenario.ACompetitionAsync(
            _fixture.DbContext, CompetitionFormat.Knockout, season: 2039);

        await CompetitionScenario.ClubsAsync(_fixture.DbContext, divisionId, 2, existing);

        await CompetitionBackfill.GiveEveryDivisionItsCompetitionAsync(_fixture.DbContext);
        await CompetitionBackfill.GiveEveryDivisionItsCompetitionAsync(_fixture.DbContext);

        var running = await _fixture.DbContext.Competitions
            .AsNoTracking()
            .Where(c => c.Entries.Any(e => e.Team.DivisionId == divisionId))
            .ToListAsync();

        running.Should().ContainSingle().Which.Id.Should().Be(existing);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// A division as it existed before this model: clubs in it, and nothing saying what they
    /// play. Written straight to the tables rather than through a handler, because no command
    /// can produce this shape any more — which is the point of backfilling it.
    /// </summary>
    private async Task<(Guid DivisionId, List<Guid> Clubs)> ADivisionOfTheOldShapeAsync(
        int clubCount)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        var divisionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _fixture.DbContext.Divisions.Add(new Division
        {
            Id = divisionId,
            Name = $"Legacy {TestIds.Code("N")}",
            ShortCode = TestIds.Code("BF"),
            Season = 2039,
            Gender = Gender.Male,
            IsActive = true,
            CreatedAt = now,
        });

        var clubs = new List<Guid>();

        for (var i = 0; i < clubCount; i++)
        {
            var id = Guid.NewGuid();
            clubs.Add(id);

            _fixture.DbContext.Teams.Add(new Team
            {
                Id = id,
                Name = $"Club {i + 1} {TestIds.Code("N")}",
                ShortCode = TestIds.Code("B"),
                DivisionId = divisionId,
                Founded = 2020,
                CreatedAt = now,
            });
        }

        await _fixture.DbContext.SaveChangesAsync();
        return (divisionId, clubs);
    }

    private async Task<Guid> AnOldFixtureAsync(Guid divisionId, Guid home, Guid away)
    {
        var id = Guid.NewGuid();

        _fixture.DbContext.MatchResults.Add(new MatchResult
        {
            Id = id,
            // No competition: the column did not exist when this row was written.
            CompetitionId = null,
            Season = 2039,
            MatchweekNumber = 1,
            MatchDate = new DateTime(2039, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            HomeTeamId = home,
            AwayTeamId = away,
            Status = MatchStatus.Scheduled,
            CreatedAt = DateTime.UtcNow,
        });

        await _fixture.DbContext.SaveChangesAsync();
        return id;
    }
}
