using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Application.Competitions;
using iDiski.Application.Competitions.Queries;
using iDiski.Application.Matches.Commands;
using iDiski.Application.MatchResults;
using iDiski.Domain.Entities;
using iDiski.Domain.Services;
using iDiski.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace iDiski.Tests.Integration.Competitions;

/// <summary>
/// The status a competition reports, end to end from its own fixtures.
///
/// CompetitionProgressTests pins the rule; this pins the three numbers the query feeds it, and
/// the thing that changed with competitions: two of them in the same division report their own
/// status, because one can be finished while the other is halfway through.
/// </summary>
public class CompetitionStatusTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public CompetitionStatusTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ACompetitionWithNoFixtures_HasNotStarted()
    {
        var (_, competitionId, _) = await AWhole(CompetitionFormat.League, 4);

        var competition = await Read(competitionId);

        competition.Status.Should().Be(CompetitionStatus.NotStarted);
        competition.MatchCount.Should().Be(0);
        competition.EntrantCount.Should().Be(4);
    }

    [Fact]
    public async Task ALeagueDrawnButUnplayed_HasNotStarted()
    {
        var (_, competitionId, _) = await AWhole(CompetitionFormat.League, 4);
        await Generate(competitionId);

        var competition = await Read(competitionId);

        competition.MatchCount.Should().Be(6);
        competition.PendingCount.Should().Be(6);
        competition.Status.Should().Be(CompetitionStatus.NotStarted);
    }

    [Fact]
    public async Task ALeagueWithEveryFixturePlayed_IsCompleted()
    {
        var (_, competitionId, _) = await AWhole(CompetitionFormat.League, 4);
        await Generate(competitionId);

        foreach (var fixture in await FixturesFor(competitionId))
            await Play(fixture);

        var competition = await Read(competitionId);

        competition.PendingCount.Should().Be(0);
        competition.Status.Should().Be(CompetitionStatus.Completed);
        competition.FinalPlayed.Should().BeFalse("a league has no final and must not wait on one");
    }

    [Fact]
    public async Task AKnockoutIsCompletedOnlyOnceItsFinalIsPlayed()
    {
        var (_, competitionId, _) = await AWhole(CompetitionFormat.Knockout, 4);
        await Generate(competitionId);

        foreach (var tie in (await FixturesFor(competitionId)).Where(f => f.KnockoutRoundSize == 4))
            await Play(tie);

        // Both semi-finals played. The final has two teams and a date, and none of that makes
        // the cup over.
        (await Read(competitionId)).Status.Should().Be(CompetitionStatus.InProgress);

        var final = (await FixturesFor(competitionId)).Single(f => f.KnockoutRoundSize == 2);
        await Play(final);

        var competition = await Read(competitionId);
        competition.FinalPlayed.Should().BeTrue();
        competition.Status.Should().Be(CompetitionStatus.Completed);
    }

    // ── What competitions were actually for ───────────────────────────────────

    [Fact]
    public async Task TwoCompetitionsInOneDivision_ReportTheirOwnStatusIndependently()
    {
        // The claim the whole change rests on: a division runs its league all season while a
        // cup inside it is won in a fortnight, and neither status describes the other.
        var divisionId = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);
        var clubs = await CompetitionScenario.ClubsAsync(_fixture.DbContext, divisionId, 4);

        var league = await CompetitionScenario.ACompetitionAsync(
            _fixture.DbContext, divisionId, CompetitionFormat.League, name: "League");
        var cup = await CompetitionScenario.ACompetitionAsync(
            _fixture.DbContext, divisionId, CompetitionFormat.Knockout, name: "Cup");

        await CompetitionScenario.EnterAsync(_fixture.DbContext, league, clubs);
        await CompetitionScenario.EnterAsync(_fixture.DbContext, cup, clubs);

        await Generate(league);
        await Generate(cup);

        // Play the cup out: two semi-finals and a final.
        foreach (var tie in (await FixturesFor(cup)).Where(f => f.KnockoutRoundSize == 4))
            await Play(tie);

        await Play((await FixturesFor(cup)).Single(f => f.KnockoutRoundSize == 2));

        // And one league fixture, so it has started but is nowhere near finished.
        await Play((await FixturesFor(league)).First());

        (await Read(cup)).Status.Should().Be(CompetitionStatus.Completed);
        (await Read(league)).Status.Should().Be(CompetitionStatus.InProgress);
    }

    [Fact]
    public async Task ACupOfSomeOfTheDivision_CountsOnlyItsOwnEntrants()
    {
        // Twenty in the division, eight in the cup — the case that was impossible to express
        // when entrants meant "whoever shares a division".
        var divisionId = await CompetitionScenario.ADivisionAsync(_fixture.DbContext);
        var clubs = await CompetitionScenario.ClubsAsync(_fixture.DbContext, divisionId, 20);

        var cup = await CompetitionScenario.ACompetitionAsync(
            _fixture.DbContext, divisionId, CompetitionFormat.Knockout, name: "Top Eight");

        await CompetitionScenario.EnterAsync(_fixture.DbContext, cup, clubs.Take(8));

        await Generate(cup);

        var competition = await Read(cup);

        competition.EntrantCount.Should().Be(8, "the division's other twelve are not in this cup");
        competition.ExternalEntrantCount.Should().Be(0);

        // Eight entrants make a bracket of seven ties, whatever the division holds.
        competition.MatchCount.Should().Be(7);

        var drawn = (await FixturesFor(cup))
            .SelectMany(f => new[] { f.HomeTeamId, f.AwayTeamId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        drawn.Should().BeSubsetOf(clubs.Take(8));
        drawn.Should().NotIntersectWith(clubs.Skip(8));
    }

    [Fact]
    public async Task ACompetitionCanFieldClubsInvitedFromAnotherDivision()
    {
        // The sponsor's cup: twelve from the division running it, plus four invited.
        var hostDivision = await CompetitionScenario.ADivisionAsync(_fixture.DbContext, name: "Host");
        var guestDivision = await CompetitionScenario.ADivisionAsync(_fixture.DbContext, name: "Guest");

        var hosts = await CompetitionScenario.ClubsAsync(_fixture.DbContext, hostDivision, 12);
        var guests = await CompetitionScenario.ClubsAsync(_fixture.DbContext, guestDivision, 4);

        var cup = await CompetitionScenario.ACompetitionAsync(
            _fixture.DbContext, hostDivision, CompetitionFormat.Knockout, name: "Sponsor Cup");

        await CompetitionScenario.EnterAsync(_fixture.DbContext, cup, hosts.Concat(guests));

        await Generate(cup);

        var competition = await Read(cup);

        competition.EntrantCount.Should().Be(16);
        competition.ExternalEntrantCount.Should().Be(4, "four clubs were invited from elsewhere");

        // The invited clubs are really in the draw, not merely listed beside it.
        var drawn = (await FixturesFor(cup))
            .SelectMany(f => new[] { f.HomeTeamId, f.AwayTeamId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        drawn.Should().IntersectWith(guests);

        // And the fixtures belong to the division running it, not to the guests' own.
        (await FixturesFor(cup)).Should().OnlyContain(f => f.DivisionId == hostDivision);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private Task<(Guid DivisionId, Guid CompetitionId, List<Guid> TeamIds)> AWhole(
        CompetitionFormat format, int teams) =>
        CompetitionScenario.AWholeAsync(_fixture.DbContext, format, teams);

    private async Task<CompetitionDto> Read(Guid competitionId)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        return await new GetCompetitionByIdQueryHandler(_fixture.DbContext)
            .Handle(new GetCompetitionByIdQuery(competitionId), CancellationToken.None)
            ?? throw new InvalidOperationException("The competition was not found.");
    }

    private Task Play(MatchResult fixture) =>
        new UpdateMatchScoreCommandHandler(_fixture.DbContext).Handle(
            new UpdateMatchScoreCommand(
                Id: fixture.Id,
                HomeScore: 2,
                AwayScore: 1,
                Status: MatchStatus.Completed,
                Notes: null),
            CancellationToken.None);

    private Task<GenerateFixturesResult> Generate(Guid competitionId, int? groups = null) =>
        new GenerateFixturesCommandHandler(_fixture.DbContext).Handle(
            new GenerateFixturesCommand(
                competitionId,
                IsHomeAndAway: false,
                StartDate: new DateTime(2040, 5, 2, 0, 0, 0, DateTimeKind.Utc),
                DaysBetweenMatchweeks: 7,
                GroupCount: groups),
            CancellationToken.None);

    private async Task<List<MatchResult>> FixturesFor(Guid competitionId)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        return await _fixture.DbContext.MatchResults
            .AsNoTracking()
            .Where(m => m.CompetitionId == competitionId)
            .OrderBy(m => m.MatchDate)
            .ToListAsync();
    }
}
