using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Application.Divisions;
using iDiski.Application.Divisions.Queries;
using iDiski.Application.Matches.Commands;
using iDiski.Application.MatchResults;
using iDiski.Domain.Entities;
using iDiski.Domain.Services;
using iDiski.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace iDiski.Tests.Integration.Divisions;

/// <summary>
/// The status a division reports, end to end from its own fixtures.
///
/// CompetitionProgressTests pins the rule; this pins the three numbers the query feeds it.
/// Both halves are needed: a correct rule given the wrong counts is a page that confidently
/// tells every visitor a finished competition is still being played.
/// </summary>
public class DivisionStatusTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public DivisionStatusTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ADivisionWithNoFixtures_HasNotStarted()
    {
        var divisionId = await ADivision(CompetitionFormat.League);
        await ClubsIn(divisionId, 4);

        var division = await Read(divisionId);

        division.Status.Should().Be(CompetitionStatus.NotStarted);
        division.MatchCount.Should().Be(0);
        division.PlayedCount.Should().Be(0);
    }

    [Fact]
    public async Task ALeagueDrawnButUnplayed_HasNotStarted()
    {
        var divisionId = await ADivision(CompetitionFormat.League);
        await ClubsIn(divisionId, 4);
        await Generate(divisionId);

        var division = await Read(divisionId);

        // Six fixtures on the calendar and nothing played is not a season under way.
        division.MatchCount.Should().Be(6);
        division.PendingCount.Should().Be(6);
        division.Status.Should().Be(CompetitionStatus.NotStarted);
    }

    [Fact]
    public async Task ALeagueMidSeason_IsInProgress()
    {
        var divisionId = await ADivision(CompetitionFormat.League);
        await ClubsIn(divisionId, 4);
        await Generate(divisionId);

        var fixtures = await FixturesFor(divisionId);
        await Play(fixtures[0]);

        var division = await Read(divisionId);

        division.PlayedCount.Should().Be(1);
        division.PendingCount.Should().Be(5);
        division.Status.Should().Be(CompetitionStatus.InProgress);
    }

    [Fact]
    public async Task ALeagueWithEveryFixturePlayed_IsCompleted()
    {
        var divisionId = await ADivision(CompetitionFormat.League);
        await ClubsIn(divisionId, 4);
        await Generate(divisionId);

        foreach (var fixture in await FixturesFor(divisionId))
            await Play(fixture);

        var division = await Read(divisionId);

        division.PendingCount.Should().Be(0);
        division.Status.Should().Be(CompetitionStatus.Completed);

        // A league has no final, and must not be waiting on one.
        division.FinalPlayed.Should().BeFalse();
    }

    [Fact]
    public async Task AKnockoutIsStillInProgressWhileItsFinalIsUnplayed()
    {
        var divisionId = await ADivision(CompetitionFormat.Knockout);
        await ClubsIn(divisionId, 4);
        await Generate(divisionId);

        // Both semi-finals played. The final now has two teams and a date, and nothing about
        // that makes the cup over.
        foreach (var tie in (await FixturesFor(divisionId)).Where(f => f.KnockoutRoundSize == 4))
            await Play(tie);

        var division = await Read(divisionId);

        division.FinalPlayed.Should().BeFalse();
        division.Status.Should().Be(CompetitionStatus.InProgress);
    }

    [Fact]
    public async Task AKnockoutIsCompletedOnceItsFinalIsPlayed()
    {
        var divisionId = await ADivision(CompetitionFormat.Knockout);
        await ClubsIn(divisionId, 4);
        await Generate(divisionId);

        foreach (var tie in (await FixturesFor(divisionId)).Where(f => f.KnockoutRoundSize == 4))
            await Play(tie);

        var final = (await FixturesFor(divisionId)).Single(f => f.KnockoutRoundSize == 2);
        await Play(final);

        var division = await Read(divisionId);

        division.FinalPlayed.Should().BeTrue();
        division.Status.Should().Be(CompetitionStatus.Completed);
    }

    [Fact]
    public async Task AGroupCompetitionIsFinishedOnceItsFinalIsPlayed_EvenWithAGroupGameLeftOver()
    {
        var divisionId = await ADivision(CompetitionFormat.GroupAndKnockout);
        await ClubsIn(divisionId, 4);
        await Generate(divisionId, groups: 2);

        var drawn = await FixturesFor(divisionId);

        // Two groups of two: one tie each. Play one and abandon the other, which is what
        // happens to a fixture nobody ever rearranges.
        var groupTies = drawn.Where(f => f.Stage == MatchStage.Group).ToList();
        groupTies.Should().HaveCount(2);
        await Play(groupTies[0]);

        // Coming through a group does not seat anybody in the bracket on its own, so the
        // semi-finals are still empty slots and a result cannot be entered against one. Seated
        // here by hand, which is what somebody running the competition does.
        await SeatQualifiers(divisionId);

        foreach (var tie in (await FixturesFor(divisionId))
            .Where(f => f.Stage == MatchStage.Knockout && f.KnockoutRoundSize == 4))
        {
            await Play(tie);
        }

        var final = (await FixturesFor(divisionId))
            .Single(f => f.Stage == MatchStage.Knockout && f.KnockoutRoundSize == 2);

        await Play(final);

        var division = await Read(divisionId);

        // One group tie is still sitting there unplayed, and the cup has been won regardless.
        division.PendingCount.Should().BeGreaterThan(0);
        division.Status.Should().Be(CompetitionStatus.Completed);
    }

    [Fact]
    public async Task TheListAndTheDetailAgree()
    {
        var divisionId = await ADivision(CompetitionFormat.League);
        await ClubsIn(divisionId, 4);
        await Generate(divisionId);

        foreach (var fixture in await FixturesFor(divisionId))
            await Play(fixture);

        var detail = await Read(divisionId);

        var listed = (await new GetDivisionsQueryHandler(_fixture.DbContext)
                .Handle(new GetDivisionsQuery(Season: 2039, IsActive: null), CancellationToken.None))
            .Single(d => d.Id == divisionId);

        // Two projections, and a screen reads one or the other depending on where you are.
        // They disagreeing is the bug this is here to catch.
        listed.Status.Should().Be(detail.Status);
        listed.PlayedCount.Should().Be(detail.PlayedCount);
        listed.PendingCount.Should().Be(detail.PendingCount);
        listed.FinalPlayed.Should().Be(detail.FinalPlayed);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Puts this division's clubs into the opening bracket round. A knockout is drawn with its
    /// entrants already in place, but a bracket fed by groups is not: the slots wait on
    /// qualification, and a fixture with an empty slot refuses a result by design.
    /// </summary>
    private async Task SeatQualifiers(Guid divisionId)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        var clubs = await _fixture.DbContext.Teams
            .Where(t => t.DivisionId == divisionId)
            .Select(t => t.Id)
            .ToListAsync();

        // Tracked on purpose: these are edited and saved.
        var openingRound = await _fixture.DbContext.MatchResults
            .Where(m => m.DivisionId == divisionId
                        && m.Stage == MatchStage.Knockout
                        && m.KnockoutRoundSize == 4)
            .OrderBy(m => m.MatchDate)
            .ToListAsync();

        for (var i = 0; i < openingRound.Count; i++)
        {
            openingRound[i].HomeTeamId = clubs[i * 2];
            openingRound[i].AwayTeamId = clubs[i * 2 + 1];
        }

        await _fixture.DbContext.SaveChangesAsync();
    }

    private async Task<DivisionDto> Read(Guid divisionId)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        return await new GetDivisionByIdQueryHandler(_fixture.DbContext)
            .Handle(new GetDivisionByIdQuery(divisionId), CancellationToken.None)
            ?? throw new InvalidOperationException("The division was not found.");
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

    private Task<GenerateFixturesResult> Generate(Guid divisionId, int? groups = null) =>
        new GenerateFixturesCommandHandler(_fixture.DbContext).Handle(
            new GenerateFixturesCommand(
                divisionId,
                Season: 2039,
                IsHomeAndAway: false,
                StartDate: new DateTime(2039, 5, 2, 0, 0, 0, DateTimeKind.Utc),
                DaysBetweenMatchweeks: 7,
                GroupCount: groups),
            CancellationToken.None);

    private async Task<List<MatchResult>> FixturesFor(Guid divisionId)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        return await _fixture.DbContext.MatchResults
            .AsNoTracking()
            .Where(m => m.DivisionId == divisionId)
            .OrderBy(m => m.MatchDate)
            .ToListAsync();
    }

    private async Task<Guid> ADivision(CompetitionFormat format)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        var divisionId = Guid.NewGuid();

        _fixture.DbContext.Divisions.Add(new Division
        {
            Id = divisionId,
            Name = $"Division {TestIds.Code("N")}",
            ShortCode = TestIds.Code("ST"),
            Season = 2039,
            Gender = Gender.Male,
            IsActive = true,
            Format = format,
            CreatedAt = DateTime.UtcNow,
        });

        await _fixture.DbContext.SaveChangesAsync();
        return divisionId;
    }

    private async Task ClubsIn(Guid divisionId, int count)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        for (var i = 0; i < count; i++)
        {
            _fixture.DbContext.Teams.Add(new Team
            {
                Id = Guid.NewGuid(),
                Name = $"Club {TestIds.Code("N")}",
                ShortCode = TestIds.Code("X"),
                DivisionId = divisionId,
                Founded = 2020,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await _fixture.DbContext.SaveChangesAsync();
    }
}
