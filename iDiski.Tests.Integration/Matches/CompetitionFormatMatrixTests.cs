using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Application.Matches.Commands;
using iDiski.Application.MatchResults;
using iDiski.Application.Standings;
using iDiski.Application.Standings.Queries;
using iDiski.Domain.Entities;
using iDiski.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace iDiski.Tests.Integration.Matches;

/// <summary>
/// The three formats, side by side, from one entry list.
///
/// Each format has its own tests elsewhere. What none of them showed is that the formats are
/// actually different from one another — and the distinction that matters most in practice is
/// between the two cup formats, which are easy to conflate:
///
///   - A <b>knockout</b> is the FA Cup. You enter, you are drawn against somebody, and you go
///     home when you lose. No groups, and no table anywhere.
///   - <b>Groups then a knockout</b> is the Champions League or the World Cup. Everybody is
///     guaranteed a few matches in a group, the group has a table, and only then does the
///     bracket start.
///
/// Sixteen entrants go through all three here, because the same sixteen clubs producing three
/// genuinely different competitions is the whole claim.
/// </summary>
public class CompetitionFormatMatrixTests : IClassFixture<IntegrationTestFixture>
{
    private const int Entrants = 16;

    private readonly IntegrationTestFixture _fixture;

    public CompetitionFormatMatrixTests(IntegrationTestFixture fixture) => _fixture = fixture;

    // ── A league ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ALeague_IsAllLeagueFixturesAndNoBracketAtAll()
    {
        var divisionId = await Generate(CompetitionFormat.League);
        var fixtures = await FixturesFor(divisionId);

        // Sixteen clubs each playing the other fifteen once.
        fixtures.Should().HaveCount(120);

        fixtures.Should().OnlyContain(m => m.Stage == MatchStage.League);
        fixtures.Should().OnlyContain(m => m.GroupName == null);
        fixtures.Should().OnlyContain(m => m.KnockoutRoundSize == null);

        // Nothing feeds anything: a league is not a bracket, so no fixture waits on another.
        fixtures.Should().OnlyContain(m => m.NextMatchId == null);

        // And every fixture knows both sides from the day it is drawn.
        fixtures.Should().OnlyContain(m => m.HomeTeamId != null && m.AwayTeamId != null);
    }

    [Fact]
    public async Task ALeague_HasATableThatResultsMove()
    {
        var divisionId = await Generate(CompetitionFormat.League);

        var fixture = (await FixturesFor(divisionId)).First();
        await Record(fixture, home: 2, away: 0);

        var table = await TableFor(divisionId);

        table.Should().HaveCount(Entrants);
        table.Sum(r => r.Played).Should().Be(2, "one match played is two clubs having played");
        table.First().Points.Should().Be(3);
    }

    // ── A knockout: the FA Cup ────────────────────────────────────────────────

    [Fact]
    public async Task AKnockout_HasNoGroupStageWhatsoever()
    {
        var divisionId = await Generate(CompetitionFormat.Knockout);
        var fixtures = await FixturesFor(divisionId);

        // The distinction from a group competition, asserted rather than assumed: you are
        // drawn straight into the bracket and there is no group anywhere in it.
        fixtures.Should().OnlyContain(m => m.Stage == MatchStage.Knockout);
        fixtures.Should().OnlyContain(m => m.GroupName == null);
        fixtures.Should().NotContain(m => m.Stage == MatchStage.Group);
    }

    [Fact]
    public async Task AKnockout_IsFifteenTiesEndingInOneFinal()
    {
        var divisionId = await Generate(CompetitionFormat.Knockout);
        var fixtures = await FixturesFor(divisionId);

        // Sixteen entrants: eight, four, two, one.
        fixtures.Should().HaveCount(15);

        fixtures.Count(m => m.KnockoutRoundSize == 16).Should().Be(8);
        fixtures.Count(m => m.KnockoutRoundSize == 8).Should().Be(4);
        fixtures.Count(m => m.KnockoutRoundSize == 4).Should().Be(2);
        fixtures.Count(m => m.KnockoutRoundSize == 2).Should().Be(1);

        // Exactly one tie leads nowhere, and that is the final.
        fixtures.Count(m => m.NextMatchId == null).Should().Be(1);
        var final = fixtures.Single(m => m.NextMatchId == null);
        (final.KnockoutRoundSize ?? 0).Should().Be(2);
    }

    [Fact]
    public async Task AKnockout_IgnoresHomeAndAway_BecauseATieIsPlayedOnce()
    {
        // Asked for home and away, which a league would honour and a bracket cannot: you
        // cannot play the losers again.
        var divisionId = await Generate(CompetitionFormat.Knockout, homeAndAway: true);

        (await FixturesFor(divisionId)).Should().HaveCount(15);
    }

    [Fact]
    public async Task AKnockout_KeepsItsTiesOutOfTheTableHoweverManyArePlayed()
    {
        var divisionId = await Generate(CompetitionFormat.Knockout);

        var tie = (await FixturesFor(divisionId)).First(m => m.KnockoutRoundSize == 16);
        await Record(tie, home: 4, away: 1);

        var table = await TableFor(divisionId);

        // Winning a tie takes you to the next round, not up a table. Four goals and a win,
        // and the standings do not move — which is why the division page shows a cup no table
        // at all rather than one reading nought everywhere.
        table.Should().OnlyContain(r => r.Played == 0);
        table.Should().OnlyContain(r => r.Points == 0);
    }

    // ── Groups then a knockout: the Champions League ──────────────────────────

    [Fact]
    public async Task GroupsThenAKnockout_HasBothStages_UnlikeEither()
    {
        var divisionId = await Generate(CompetitionFormat.GroupAndKnockout, groups: 4);
        var fixtures = await FixturesFor(divisionId);

        var group = fixtures.Where(m => m.Stage == MatchStage.Group).ToList();
        var bracket = fixtures.Where(m => m.Stage == MatchStage.Knockout).ToList();

        group.Should().NotBeEmpty("this is the half a straight knockout does not have");
        bracket.Should().NotBeEmpty("and this is the half a league does not have");

        // Four groups of four, each playing itself out: six ties a group.
        group.Should().HaveCount(24);
        group.Should().OnlyContain(m => m.GroupName != null);

        // Two from each of four groups is eight qualifiers: four, two, one.
        bracket.Should().HaveCount(7);
        bracket.Should().OnlyContain(m => m.GroupName == null);
    }

    [Fact]
    public async Task GroupsThenAKnockout_KnowsItsGroupTeamsButNotItsQualifiers()
    {
        var divisionId = await Generate(CompetitionFormat.GroupAndKnockout, groups: 4);
        var fixtures = await FixturesFor(divisionId);

        // Everyone knows their group opponents on day one.
        fixtures.Where(m => m.Stage == MatchStage.Group)
            .Should().OnlyContain(m => m.HomeTeamId != null && m.AwayTeamId != null);

        // Nobody has come through yet, so the bracket is drawn up empty — which is the point
        // of drawing it at all: the dates and rounds are settled before the names are.
        fixtures.Where(m => m.Stage == MatchStage.Knockout)
            .Should().OnlyContain(m => m.HomeTeamId == null && m.AwayTeamId == null);
    }

    [Fact]
    public async Task GroupsThenAKnockout_CountsGroupResultsAndIgnoresBracketOnes()
    {
        var divisionId = await Generate(CompetitionFormat.GroupAndKnockout, groups: 4);

        var groupTie = (await FixturesFor(divisionId)).First(m => m.Stage == MatchStage.Group);
        await Record(groupTie, home: 1, away: 0);

        var table = await TableFor(divisionId);

        // A group is a league in miniature, so its results count. Bracket ties never do.
        table.Sum(r => r.Played).Should().Be(2);
    }

    // ── The three, side by side ───────────────────────────────────────────────

    [Fact]
    public async Task TheSameSixteenClubs_MakeThreeDifferentCompetitions()
    {
        var league = await FixturesFor(await Generate(CompetitionFormat.League));
        var cup = await FixturesFor(await Generate(CompetitionFormat.Knockout));
        var groups = await FixturesFor(
            await Generate(CompetitionFormat.GroupAndKnockout, groups: 4));

        // Same entry list, three genuinely different seasons' worth of football.
        league.Count.Should().Be(120);
        cup.Count.Should().Be(15);
        groups.Count.Should().Be(31);

        // And the stages they are made of are what tells them apart.
        Stages(league).Should().Equal(MatchStage.League);
        Stages(cup).Should().Equal(MatchStage.Knockout);
        Stages(groups).Should().Equal(MatchStage.Group, MatchStage.Knockout);
    }

    [Fact]
    public async Task ACupAndAGroupCompetition_AreNotTheSameThing()
    {
        var cup = await FixturesFor(await Generate(CompetitionFormat.Knockout));
        var champions = await FixturesFor(
            await Generate(CompetitionFormat.GroupAndKnockout, groups: 4));

        // The distinction worth having in one assertion: an FA Cup has nobody in a group, a
        // Champions League has everybody in one before a ball is kicked in its bracket.
        cup.Should().OnlyContain(m => m.GroupName == null);
        champions.Count(m => m.GroupName != null).Should().Be(24);

        // Both end in a single final, which is the part they do share.
        cup.Count(m => m.KnockoutRoundSize == 2).Should().Be(1);
        champions.Count(m => m.KnockoutRoundSize == 2).Should().Be(1);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static List<MatchStage> Stages(IEnumerable<MatchResult> fixtures) =>
        fixtures.Select(m => m.Stage).Distinct().OrderBy(s => s).ToList();

    private async Task<List<MatchResult>> FixturesFor(Guid divisionId)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        return await _fixture.DbContext.MatchResults
            .AsNoTracking()
            .Where(m => m.DivisionId == divisionId)
            .ToListAsync();
    }

    private async Task<List<StandingDto>> TableFor(Guid divisionId)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        var result = await new GetLeagueStandingsQueryHandler(_fixture.DbContext).Handle(
            new GetLeagueStandingsQuery(Season: 2026, DivisionId: divisionId),
            CancellationToken.None);

        return result.Table.ToList();
    }

    private Task Record(MatchResult match, int home, int away) =>
        new UpdateMatchScoreCommandHandler(_fixture.DbContext).Handle(
            new UpdateMatchScoreCommand(match.Id, home, away, MatchStatus.Completed, null),
            CancellationToken.None);

    private async Task<Guid> Generate(
        CompetitionFormat format,
        int? groups = null,
        bool homeAndAway = false)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        var divisionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _fixture.DbContext.Divisions.Add(new Division
        {
            Id = divisionId,
            Name = $"{format} of {Entrants}",
            ShortCode = TestIds.Code("FM"),
            Season = 2026,
            Gender = Gender.Male,
            IsActive = true,
            Format = format,
            CreatedAt = now,
        });

        for (var i = 0; i < Entrants; i++)
        {
            _fixture.DbContext.Teams.Add(new Team
            {
                Id = Guid.NewGuid(),
                Name = $"Club {i + 1}",
                ShortCode = TestIds.Code("C"),
                DivisionId = divisionId,
                Founded = 2020,
                CreatedAt = now,
            });
        }

        await _fixture.DbContext.SaveChangesAsync();

        await new GenerateFixturesCommandHandler(_fixture.DbContext).Handle(
            new GenerateFixturesCommand(
                divisionId,
                Season: 2026,
                IsHomeAndAway: homeAndAway,
                StartDate: new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                DaysBetweenMatchweeks: 7,
                GroupCount: groups),
            CancellationToken.None);

        return divisionId;
    }
}
