using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Application.DataManagement.Commands;
using iDiski.Application.Divisions;
using iDiski.Application.Divisions.Commands;
using iDiski.Application.Matches.Commands;
using iDiski.Domain.Entities;
using iDiski.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace iDiski.Tests.Integration.Divisions;

/// <summary>
/// Removing a division, now that a division is its clubs and nothing else.
///
/// Both paths used to reach into competitions, back when a division owned them. A division
/// owns none: what its clubs play stands on its own and is contested by whoever was entered,
/// which may be clubs from three other divisions. So deleting one asks only about its clubs,
/// and clearing one takes its clubs and their places out of competitions without taking the
/// competitions themselves.
/// </summary>
public class DivisionLifecycleTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public DivisionLifecycleTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ADivisionWithClubsInItIsNotDeleted()
    {
        var divisionId = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);
        await CompetitionScenario.ClubsAsync(_fixture.DbContext, divisionId, 3);

        var delete = async () => await new DeleteDivisionCommandHandler(_fixture.DbContext)
            .Handle(new DeleteDivisionCommand(divisionId), CancellationToken.None);

        (await delete.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("clubs");

        (await _fixture.DbContext.Divisions.AsNoTracking().AnyAsync(d => d.Id == divisionId))
            .Should().BeTrue();
    }

    [Fact]
    public async Task AnEmptyDivisionIsDeletedEvenWhileCompetitionsAreBeingPlayed()
    {
        // The rule that changed. A competition being played elsewhere is no business of this
        // division's: it holds no clubs, so there is nothing left in it to lose.
        var divisionId = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);
        var elsewhere = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);

        var competitionId = await CompetitionScenario.ACompetitionAsync(_fixture.DbContext);
        await CompetitionScenario.ClubsAsync(_fixture.DbContext, elsewhere, 2, competitionId);

        await new DeleteDivisionCommandHandler(_fixture.DbContext)
            .Handle(new DeleteDivisionCommand(divisionId), CancellationToken.None);

        (await _fixture.DbContext.Divisions.AsNoTracking().AnyAsync(d => d.Id == divisionId))
            .Should().BeFalse();

        (await _fixture.DbContext.Competitions.AsNoTracking().AnyAsync(c => c.Id == competitionId))
            .Should().BeTrue("a competition is not a division's to take with it");
    }

    [Fact]
    public async Task ClearingADivisionTakesItsClubsAndTheirPlacesButNotTheCompetition()
    {
        var (divisionId, competitionId, _) = await CompetitionScenario.AWholeAsync(
            _fixture.DbContext, CompetitionFormat.League, 4);

        await new GenerateFixturesCommandHandler(_fixture.DbContext).Handle(
            new GenerateFixturesCommand(
                competitionId,
                IsHomeAndAway: false,
                StartDate: new DateTime(2040, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                DaysBetweenMatchweeks: 7),
            CancellationToken.None);

        // Everything is there before the clear, or the assertions afterwards prove nothing.
        (await _fixture.DbContext.MatchResults.AsNoTracking()
            .CountAsync(m => m.CompetitionId == competitionId)).Should().Be(6);

        _fixture.DbContext.ChangeTracker.Clear();

        await new ClearDivisionCommandHandler(_fixture.DbContext)
            .Handle(new ClearDivisionCommand(divisionId), CancellationToken.None);

        (await _fixture.DbContext.Teams.AsNoTracking()
            .AnyAsync(t => t.DivisionId == divisionId))
            .Should().BeFalse();

        (await _fixture.DbContext.CompetitionEntries.AsNoTracking()
            .AnyAsync(e => e.CompetitionId == competitionId))
            .Should().BeFalse("a club that no longer exists holds no place in anything");

        (await _fixture.DbContext.MatchResults.AsNoTracking()
            .AnyAsync(m => m.CompetitionId == competitionId))
            .Should().BeFalse("its fixtures went with the clubs that were to play them");

        // The competition itself survives, empty. A cup contested across three divisions is
        // not abandoned because one of them was cleared — it has fewer entrants, which is the
        // truth of what happened.
        (await _fixture.DbContext.Competitions.AsNoTracking()
            .AnyAsync(c => c.Id == competitionId))
            .Should().BeTrue();
    }
}
