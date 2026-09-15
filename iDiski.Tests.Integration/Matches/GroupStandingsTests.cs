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
/// A table for one group of a group stage.
///
/// Merging the groups into a single table ranks teams against opponents they have never
/// played, which is not a table of anything — and it is what the division page did until the
/// standings query learned to narrow by group.
/// </summary>
public class GroupStandingsTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public GroupStandingsTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task AGroupsTable_HoldsOnlyThatGroupsTeams()
    {
        var competitionId = await AGroupStageOf(8, groups: 2);

        var groupA = await TableFor(competitionId, "A");
        var groupB = await TableFor(competitionId, "B");

        groupA.Should().HaveCount(4);
        groupB.Should().HaveCount(4);

        // Nobody is in two groups, and between them they account for everyone.
        groupA.Select(r => r.TeamId).Should().NotIntersectWith(groupB.Select(r => r.TeamId));
        groupA.Concat(groupB).Select(r => r.TeamId).Distinct().Should().HaveCount(8,
            "between them the groups account for every entrant, each exactly once");
    }

    [Fact]
    public async Task ATeamThatHasNotPlayedYet_StillAppearsInItsGroup()
    {
        var competitionId = await AGroupStageOf(4, groups: 2);

        var groupA = await TableFor(competitionId, "A");

        // Built from who was drawn rather than from who has played, otherwise a side sits
        // missing from its own group's table instead of bottom of it on nought.
        groupA.Should().HaveCount(2);
        groupA.Should().OnlyContain(r => r.Played == 0);
    }

    [Fact]
    public async Task AResultInOneGroup_DoesNotMoveTheOther()
    {
        var competitionId = await AGroupStageOf(8, groups: 2);

        var fixture = await _fixture.DbContext.MatchResults
            .AsNoTracking()
            .Where(m => m.CompetitionId == competitionId && m.GroupName == "A")
            .FirstAsync();

        await Record(fixture, home: 3, away: 0);

        var groupA = await TableFor(competitionId, "A");
        var groupB = await TableFor(competitionId, "B");

        groupA.Sum(r => r.Played).Should().Be(2, "one match played is two teams having played");
        groupB.Sum(r => r.Played).Should().Be(0, "group B has not kicked a ball");

        groupA.First().Points.Should().Be(3);
    }

    [Fact]
    public async Task AskingWithoutAGroup_StillGivesTheWholeDivision()
    {
        var competitionId = await AGroupStageOf(8, groups: 2);

        var everyone = await TableFor(competitionId, group: null);

        // The existing behaviour, unchanged: a league asks for no group and gets its table.
        everyone.Should().HaveCount(8);
    }

    [Fact]
    public async Task TheKnockoutTiesAreNeverInAGroupTable()
    {
        var competitionId = await AGroupStageOf(8, groups: 2);

        var bracket = await _fixture.DbContext.MatchResults
            .AsNoTracking()
            .Where(m => m.CompetitionId == competitionId && m.Stage == MatchStage.Knockout)
            .ToListAsync();

        bracket.Should().NotBeEmpty("a group stage feeds a bracket");

        // Every bracket fixture carries no group, so none of them can reach a group's table
        // even before the standings query excludes knockout ties outright.
        bracket.Should().OnlyContain(m => m.GroupName == null);
    }

    [Fact]
    public async Task ThirtyTwoTeams_MakeEightGroupsOfFourAndARoundOfSixteen()
    {
        // The shape an organiser actually asks for, pinned end to end: thirty-two entrants,
        // eight groups of four named A to H, the top two from each going through to a last
        // sixteen.
        var competitionId = await AGroupStageOf(32, groups: 8);

        var groupFixtures = await _fixture.DbContext.MatchResults
            .AsNoTracking()
            .Where(m => m.CompetitionId == competitionId && m.Stage == MatchStage.Group)
            .ToListAsync();

        var names = groupFixtures
            .Where(m => m.GroupName is not null)
            .Select(m => m.GroupName!)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        names.Should().Equal("A", "B", "C", "D", "E", "F", "G", "H");

        foreach (var name in names)
        {
            var table = await TableFor(competitionId, name);
            table.Should().HaveCount(4, $"group {name} holds four of the thirty-two");
        }

        // Four teams playing each other once is six fixtures, eight times over.
        groupFixtures.Should().HaveCount(48);

        var openingRound = await _fixture.DbContext.MatchResults
            .AsNoTracking()
            .Where(m => m.CompetitionId == competitionId && m.Stage == MatchStage.Knockout)
            .ToListAsync();

        // Sixteen qualifiers: a round of sixteen, then eight, four, two.
        openingRound.Max(m => m.KnockoutRoundSize ?? 0).Should().Be(16);
        openingRound.Count(m => m.KnockoutRoundSize == 16).Should().Be(8);
        openingRound.Should().HaveCount(15, "a bracket of sixteen is fifteen ties in all");
    }

    [Fact]
    public async Task TheGroupsAreLetteredFromA()
    {
        var competitionId = await AGroupStageOf(20, groups: 5);

        var names = await _fixture.DbContext.MatchResults
            .AsNoTracking()
            .Where(m => m.CompetitionId == competitionId && m.Stage == MatchStage.Group
                        && m.GroupName != null)
            .Select(m => m.GroupName!)
            .Distinct()
            .ToListAsync();

        names.OrderBy(n => n).Should().Equal("A", "B", "C", "D", "E");
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task<List<StandingDto>> TableFor(Guid competitionId, string? group)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        var result = await new GetLeagueStandingsQueryHandler(_fixture.DbContext).Handle(
            new GetLeagueStandingsQuery(Season: 2026, CompetitionId: competitionId, GroupName: group),
            CancellationToken.None);

        return result.Table.ToList();
    }

    private Task Record(MatchResult match, int home, int away) =>
        new UpdateMatchScoreCommandHandler(_fixture.DbContext).Handle(
            new UpdateMatchScoreCommand(match.Id, home, away, MatchStatus.Completed, null),
            CancellationToken.None);

    private async Task<Guid> AGroupStageOf(int teamCount, int groups)
    {
        _fixture.DbContext.ChangeTracker.Clear();

        var divisionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _fixture.DbContext.Divisions.Add(new Division
        {
            Id = divisionId,
            Name = $"Groups of {teamCount}",
            ShortCode = TestIds.Code("GS"),
            Season = 2026,
            Gender = Gender.Male,
            IsActive = true,
            CreatedAt = now,
        });

        var competitionId = Guid.NewGuid();

        _fixture.DbContext.Competitions.Add(new Competition
        {
            Id = competitionId,
            DivisionId = divisionId,
            Name = $"Groups of {teamCount}",
            ShortCode = TestIds.Code("GS"),
            Season = 2026,
            Format = CompetitionFormat.GroupAndKnockout,
            IsActive = true,
            CreatedAt = now,
        });

        for (var i = 0; i < teamCount; i++)
        {
            var teamId = Guid.NewGuid();

            _fixture.DbContext.Teams.Add(new Team
            {
                Id = teamId,
                Name = $"Side {i + 1}",
                ShortCode = TestIds.Code("G"),
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

        await new GenerateFixturesCommandHandler(_fixture.DbContext).Handle(
            new GenerateFixturesCommand(
                competitionId,
                IsHomeAndAway: false,
                StartDate: new DateTime(2026, 7, 4, 0, 0, 0, DateTimeKind.Utc),
                DaysBetweenMatchweeks: 1,
                GroupCount: groups),
            CancellationToken.None);

        return competitionId;
    }
}
