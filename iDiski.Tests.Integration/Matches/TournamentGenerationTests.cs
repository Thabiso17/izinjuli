using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Application.Common.Exceptions;
using iDiski.Application.Matches.Commands;
using iDiski.Domain.Entities;
using iDiski.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace iDiski.Tests.Integration.Matches;

/// <summary>
/// Knockout and group competitions, which a division now chooses between.
///
/// A bracket is harder to check than a league: it is not enough to count the fixtures, because
/// a bracket that is wrongly linked still has the right number of them. What makes it a bracket
/// is that every fixture except the final feeds exactly one other, that the rounds halve, and
/// that a team drawn a bye is already standing in the next round rather than playing a fixture
/// against nobody.
/// </summary>
public class TournamentGenerationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public TournamentGenerationTests(IntegrationTestFixture fixture) => _fixture = fixture;

    // ── Knockout ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task EightTeams_GiveAQuarterFinalSemiFinalAndFinal()
    {
        var divisionId = await CreateCompetitionAsync(CompetitionFormat.Knockout, teamCount: 8);

        var result = await Generate(divisionId);

        // Four, then two, then one: seven matches settle eight teams.
        result.FixturesGenerated.Should().Be(7);

        var fixtures = await FixturesFor(divisionId);
        fixtures.Should().OnlyContain(f => f.Stage == MatchStage.Knockout);

        fixtures.Count(f => f.KnockoutRoundSize == 8).Should().Be(4);
        fixtures.Count(f => f.KnockoutRoundSize == 4).Should().Be(2);
        fixtures.Count(f => f.KnockoutRoundSize == 2).Should().Be(1);
    }

    [Fact]
    public async Task EveryFixtureFeedsTheNextOne_AndTheFinalFeedsNothing()
    {
        var divisionId = await CreateCompetitionAsync(CompetitionFormat.Knockout, teamCount: 8);

        await Generate(divisionId);

        var fixtures = await FixturesFor(divisionId);
        var final = fixtures.Single(f => f.KnockoutRoundSize == 2);

        final.NextMatchId.Should().BeNull("there is nothing after the final");

        foreach (var fixture in fixtures.Where(f => f.KnockoutRoundSize != 2))
        {
            fixture.NextMatchId.Should().NotBeNull(
                "a knockout fixture with nowhere to send its winner is a dead end");
            fixture.NextMatchSlot.Should().NotBeNull();
        }

        // Exactly two fixtures feed each later one, and they take opposite sides of it, or a
        // winner would overwrite another winner.
        foreach (var feeding in fixtures.Where(f => f.NextMatchId is not null)
                     .GroupBy(f => f.NextMatchId))
        {
            feeding.Should().HaveCount(2);
            feeding.Select(f => f.NextMatchSlot).Should().OnlyHaveUniqueItems();
        }
    }

    [Fact]
    public async Task AnEntryListThatIsNotAPowerOfTwo_SeatsTheByesInTheNextRound()
    {
        // Six entrants fill a bracket of eight, so two of them sit out the first round.
        var divisionId = await CreateCompetitionAsync(CompetitionFormat.Knockout, teamCount: 6);

        await Generate(divisionId);

        var fixtures = await FixturesFor(divisionId);

        var openingRound = fixtures.Where(f => f.KnockoutRoundSize == 8).ToList();
        openingRound.Should().HaveCount(2, "the two byes are not fixtures anybody plays");
        openingRound.Should().OnlyContain(f => f.HomeTeamId != null && f.AwayTeamId != null);

        // The teams given byes are already in the second round rather than waiting on a result.
        var secondRound = fixtures.Where(f => f.KnockoutRoundSize == 4).ToList();
        secondRound.SelectMany(f => new[] { f.HomeTeamId, f.AwayTeamId })
            .Count(id => id is not null)
            .Should().Be(2, "one bye lands in each half of the draw");
    }

    [Fact]
    public async Task LaterRoundsAreScheduledWithNobodyInThemYet()
    {
        var divisionId = await CreateCompetitionAsync(CompetitionFormat.Knockout, teamCount: 8);

        await Generate(divisionId);

        var fixtures = await FixturesFor(divisionId);
        var final = fixtures.Single(f => f.KnockoutRoundSize == 2);

        // The whole point of building the bracket up front: the final has a date before it has
        // teams, so an organiser can book a pitch for it on day one.
        final.HomeTeamId.Should().BeNull();
        final.AwayTeamId.Should().BeNull();
        final.MatchDate.Should().BeAfter(fixtures.Min(f => f.MatchDate));
    }

    [Fact]
    public async Task AWeekendTournament_PutsEveryRoundOnTheSameDay()
    {
        var divisionId = await CreateCompetitionAsync(CompetitionFormat.Knockout, teamCount: 4);

        // Nought days between rounds. This could not be expressed at all before: the rule
        // insisted on at least one day, so an amateur tournament run off over a weekend had no
        // way to say so.
        await Generate(divisionId, daysBetween: 0);

        var fixtures = await FixturesFor(divisionId);
        fixtures.Select(f => f.MatchDate.Date).Distinct().Should().HaveCount(1);
    }

    // ── Groups feeding a bracket ──────────────────────────────────────────────

    [Fact]
    public async Task GroupsArePlayedOut_AndTheBracketWaitsForTheirQualifiers()
    {
        var divisionId = await CreateCompetitionAsync(
            CompetitionFormat.GroupAndKnockout, teamCount: 8);

        var result = await Generate(divisionId, groupCount: 2, advancing: 2);

        var fixtures = await FixturesFor(divisionId);

        // Two groups of four: six fixtures each. Four qualifiers: two semi-finals and a final.
        var group = fixtures.Where(f => f.Stage == MatchStage.Group).ToList();
        var bracket = fixtures.Where(f => f.Stage == MatchStage.Knockout).ToList();

        group.Should().HaveCount(12);
        bracket.Should().HaveCount(3);
        result.FixturesGenerated.Should().Be(15);

        group.Select(f => f.GroupName).Distinct().Should().BeEquivalentTo(new[] { "A", "B" });
        group.Should().OnlyContain(f => f.HomeTeamId != null && f.AwayTeamId != null);

        // Nobody has qualified yet, so every bracket slot is empty.
        bracket.Should().OnlyContain(f => f.HomeTeamId == null && f.AwayTeamId == null);
    }

    [Fact]
    public async Task TheBracketStartsAfterTheGroupsHaveFinished()
    {
        var divisionId = await CreateCompetitionAsync(
            CompetitionFormat.GroupAndKnockout, teamCount: 8);

        await Generate(divisionId, groupCount: 2, advancing: 2);

        var fixtures = await FixturesFor(divisionId);

        var lastGroupWeek = fixtures.Where(f => f.Stage == MatchStage.Group)
            .Max(f => f.MatchweekNumber);
        var firstBracketWeek = fixtures.Where(f => f.Stage == MatchStage.Knockout)
            .Min(f => f.MatchweekNumber);

        firstBracketWeek.Should().BeGreaterThan(lastGroupWeek,
            "a knockout round landing on top of a group's last fixtures cannot be played");
    }

    [Fact]
    public async Task AnUnevenEntryList_SpreadsAcrossTheGroupsRatherThanLoadingTheLast()
    {
        // Seven teams over two groups: four and three, not four and three by accident.
        var divisionId = await CreateCompetitionAsync(
            CompetitionFormat.GroupAndKnockout, teamCount: 7);

        await Generate(divisionId, groupCount: 2, advancing: 2);

        var fixtures = await FixturesFor(divisionId);

        var teamsPerGroup = fixtures
            .Where(f => f.Stage == MatchStage.Group)
            .GroupBy(f => f.GroupName)
            .ToDictionary(
                g => g.Key!,
                g => g.SelectMany(f => new[] { f.HomeTeamId, f.AwayTeamId }).Distinct().Count());

        teamsPerGroup.Values.Max().Should().BeLessThanOrEqualTo(
            teamsPerGroup.Values.Min() + 1,
            "dealing the teams out keeps the groups within one of each other");
    }

    [Fact]
    public async Task MoreGroupsThanCanBeFilled_IsRefused()
    {
        var divisionId = await CreateCompetitionAsync(
            CompetitionFormat.GroupAndKnockout, teamCount: 4);

        // Four teams cannot make three groups of two.
        var generate = async () => await Generate(divisionId, groupCount: 3, advancing: 1);

        await generate.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AdvancingMoreThanAGroupHolds_IsRefused()
    {
        var divisionId = await CreateCompetitionAsync(
            CompetitionFormat.GroupAndKnockout, teamCount: 4);

        // Two groups of two cannot each send three through.
        var generate = async () => await Generate(divisionId, groupCount: 2, advancing: 3);

        await generate.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task TheOrganiserChoosesHowManyComeOutOfEachGroup()
    {
        var divisionId = await CreateCompetitionAsync(
            CompetitionFormat.GroupAndKnockout, teamCount: 8);

        // One from each of four groups is four qualifiers: two semi-finals and a final.
        await Generate(divisionId, groupCount: 4, advancing: 1);

        var bracket = (await FixturesFor(divisionId))
            .Where(f => f.Stage == MatchStage.Knockout)
            .ToList();

        bracket.Should().HaveCount(3);
        bracket.Count(f => f.KnockoutRoundSize == 4).Should().Be(2);
        bracket.Count(f => f.KnockoutRoundSize == 2).Should().Be(1);

        // Sending two from each of those groups instead would double the bracket, which is the
        // point of letting the organiser choose.
        var wider = await CreateCompetitionAsync(CompetitionFormat.GroupAndKnockout, teamCount: 8);
        await Generate(wider, groupCount: 2, advancing: 2);

        (await FixturesFor(wider)).Count(f => f.Stage == MatchStage.Knockout).Should().Be(3);
    }

    [Fact]
    public async Task ALeagueDivision_IsStillARoundRobin()
    {
        var divisionId = await CreateCompetitionAsync(CompetitionFormat.League, teamCount: 4);

        await Generate(divisionId);

        var fixtures = await FixturesFor(divisionId);

        fixtures.Should().HaveCount(6);
        fixtures.Should().OnlyContain(f => f.Stage == MatchStage.League);
        fixtures.Should().OnlyContain(f => f.KnockoutRoundSize == null);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private Task<GenerateFixturesResult> Generate(
        Guid competitionId,
        int daysBetween = 7,
        int? groupCount = null,
        int advancing = 2) =>
        new GenerateFixturesCommandHandler(_fixture.DbContext).Handle(
            new GenerateFixturesCommand(
                competitionId,
                IsHomeAndAway: false,
                StartDate: new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc),
                DaysBetweenMatchweeks: daysBetween,
                ReplaceExisting: false,
                GroupCount: groupCount,
                TeamsAdvancingPerGroup: advancing),
            CancellationToken.None);

    private async Task<List<MatchResult>> FixturesFor(Guid divisionId) =>
        await _fixture.DbContext.MatchResults
            .AsNoTracking()
            .Where(m => m.CompetitionId == divisionId)
            .ToListAsync();

    private async Task<Guid> CreateCompetitionAsync(CompetitionFormat format, int teamCount)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        var divisionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _fixture.DbContext.Divisions.Add(new Division
        {
            Id = divisionId,
            Name = $"{format} of {teamCount}",
            ShortCode = TestIds.Code("TN"),
            Season = 2026,
            Gender = Gender.Male,
            IsActive = true,
            CreatedAt = now,
        });

        // The division is the pool; the competition is what gets played. Everybody in the
        // division is entered, which is what this file assumes throughout.
        var competitionId = Guid.NewGuid();

        _fixture.DbContext.Competitions.Add(new Competition
        {
            Id = competitionId,
            DivisionId = divisionId,
            Name = $"Competition {TestIds.Code("N")}",
            ShortCode = TestIds.Code("TN"),
            Season = 2026,
            Format = format,
            IsActive = true,
            CreatedAt = now,
        });

        for (var i = 0; i < teamCount; i++)
        {
            var teamId = Guid.NewGuid();

            _fixture.DbContext.Teams.Add(new Team
            {
                Id = teamId,
                Name = $"Entrant {i + 1}",
                ShortCode = TestIds.Code("T"),
                DivisionId = divisionId,
                Founded = 2020,
                CreatedAt = now,
            });

            _fixture.DbContext.CompetitionEntries.Add(new CompetitionEntry
            {
                Id = Guid.NewGuid(),
                CompetitionId = competitionId,
                TeamId = teamId,
                CreatedAt = now,
            });
        }

        await _fixture.DbContext.SaveChangesAsync();
        return competitionId;
    }
}
