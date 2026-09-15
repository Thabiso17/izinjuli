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
/// Removing a division, now that it is a pool of clubs rather than the competition itself.
///
/// Both paths gained a competition-shaped hole when the model changed. Deleting checked teams
/// and matches, neither of which covers a division whose clubs have all left but which still
/// runs a cup for invited sides. Clearing walked the division's teams, which would have left
/// competitions and entry lists behind pointing at a division that no longer existed.
/// </summary>
public class DivisionLifecycleTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public DivisionLifecycleTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ADivisionRunningACompetitionIsNotDeleted()
    {
        // Deliberately empty of clubs: the old guard asked about teams and matches, and this
        // division has neither. What it has is a competition.
        var divisionId = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);
        await CompetitionScenario.ACompetitionAsync(_fixture.DbContext, divisionId);

        var delete = async () => await new DeleteDivisionCommandHandler(_fixture.DbContext)
            .Handle(new DeleteDivisionCommand(divisionId), CancellationToken.None);

        (await delete.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("competition");

        (await _fixture.DbContext.Divisions.AsNoTracking().AnyAsync(d => d.Id == divisionId))
            .Should().BeTrue();
    }

    [Fact]
    public async Task AnEmptyDivisionRunningNothingIsDeleted()
    {
        // The other half, so the guard above is a rule rather than a blanket refusal.
        var divisionId = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);

        await new DeleteDivisionCommandHandler(_fixture.DbContext)
            .Handle(new DeleteDivisionCommand(divisionId), CancellationToken.None);

        (await _fixture.DbContext.Divisions.AsNoTracking().AnyAsync(d => d.Id == divisionId))
            .Should().BeFalse();
    }

    [Fact]
    public async Task ClearingADivisionTakesItsCompetitionsAndTheirEntryListsWithIt()
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

        (await _fixture.DbContext.Competitions.AsNoTracking()
            .AnyAsync(c => c.DivisionId == divisionId))
            .Should().BeFalse("a competition belongs to the division that was cleared");

        (await _fixture.DbContext.CompetitionEntries.AsNoTracking()
            .AnyAsync(e => e.CompetitionId == competitionId))
            .Should().BeFalse("an entry list outlives nothing");

        (await _fixture.DbContext.MatchResults.AsNoTracking()
            .AnyAsync(m => m.CompetitionId == competitionId))
            .Should().BeFalse();

        (await _fixture.DbContext.Teams.AsNoTracking()
            .AnyAsync(t => t.DivisionId == divisionId))
            .Should().BeFalse();
    }
}
