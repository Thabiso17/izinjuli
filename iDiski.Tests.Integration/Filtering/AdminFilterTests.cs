using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Application.MatchResults;
using iDiski.Application.Players.Queries;
using iDiski.Application.Suspensions;
using iDiski.Application.Suspensions.Queries;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
using iDiski.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace iDiski.Tests.Integration.Filtering;

/// <summary>
/// The admin pages narrow by division and team, and all three of these were reported as
/// showing the wrong rows.
///
/// They shared a cause: a filter the screen offered that the query behind it did not have. The
/// players list sent only a team, so clearing the team widened the grid to the whole league
/// rather than back to the chosen division. The matches page had always sent a division the
/// endpoint never bound, so choosing one changed nothing. Suspensions could not narrow by team
/// at all.
///
/// A filter that is silently dropped looks exactly like a filter that matched everything, which
/// is why none of it was obvious from the screen. These assert on what actually comes back.
/// </summary>
public class AdminFilterTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public AdminFilterTests(IntegrationTestFixture fixture) => _fixture = fixture;

    // ── Players ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Players_NarrowToADivision_WhenNoTeamIsChosen()
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);
        // PlayerA already plays for TeamA in DivisionOne. Give DivisionTwo someone of its own.
        var outsider = await AddPlayerAsync(scenario.TeamCId, jersey: 7);

        var inDivisionOne = await new GetPlayersQueryHandler(_fixture.DbContext).Handle(
            new GetPlayersQuery(DivisionId: scenario.DivisionOneId), CancellationToken.None);

        inDivisionOne.Should().Contain(p => p.Id == scenario.PlayerAId);
        inDivisionOne.Should().NotContain(p => p.Id == outsider.Id,
            "that player's team plays in another division");
    }

    [Fact]
    public async Task Players_WithNoFilters_StillReturnTheWholeLeague()
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);
        var outsider = await AddPlayerAsync(scenario.TeamCId, jersey: 8);

        var all = await new GetPlayersQueryHandler(_fixture.DbContext).Handle(
            new GetPlayersQuery(), CancellationToken.None);

        all.Should().Contain(p => p.Id == scenario.PlayerAId);
        all.Should().Contain(p => p.Id == outsider.Id);
    }

    [Fact]
    public async Task Players_TeamAndDivisionTogether_NarrowToThatTeam()
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);
        var teamBPlayer = await AddPlayerAsync(scenario.TeamBId, jersey: 11);

        var result = await new GetPlayersQueryHandler(_fixture.DbContext).Handle(
            new GetPlayersQuery(TeamId: scenario.TeamAId, DivisionId: scenario.DivisionOneId),
            CancellationToken.None);

        result.Should().Contain(p => p.Id == scenario.PlayerAId);
        result.Should().NotContain(p => p.Id == teamBPlayer.Id,
            "both teams are in that division, so the narrower filter has to win");
    }

    // ── Matches ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Matches_NarrowToADivision()
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);

        var inDivisionOne = await AddMatchAsync(
            scenario.DivisionOneId, scenario.TeamAId, scenario.TeamBId);
        var elsewhere = await AddMatchAsync(
            scenario.DivisionTwoId, scenario.TeamCId, scenario.TeamAId);

        var result = await new GetFixturesQueryHandler(_fixture.DbContext).Handle(
            new GetFixturesQuery(Season: 2026, PageSize: 100, DivisionId: scenario.DivisionOneId),
            CancellationToken.None);

        result.Items.Should().Contain(m => m.Id == inDivisionOne.Id);
        result.Items.Should().NotContain(m => m.Id == elsewhere.Id,
            "the page has offered this filter all along without it doing anything");
    }

    [Fact]
    public async Task Matches_NarrowToACompetition_WithinTheSameDivision()
    {
        // The filter a division cannot express. Both fixtures are in the same division — its
        // league and its cup — so narrowing by division returns both and narrowing by
        // competition is the only thing that separates them.
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);

        var cup = await AddCompetitionAsync(CompetitionFormat.Knockout);

        var inTheLeague = await AddMatchAsync(
            scenario.DivisionOneId, scenario.TeamAId, scenario.TeamBId);
        var inTheCup = await AddMatchAsync(
            scenario.DivisionOneId, scenario.TeamAId, scenario.TeamBId, competitionId: cup);

        var result = await new GetFixturesQueryHandler(_fixture.DbContext).Handle(
            new GetFixturesQuery(Season: 2026, PageSize: 100, CompetitionId: cup),
            CancellationToken.None);

        result.Items.Should().Contain(m => m.Id == inTheCup.Id);
        result.Items.Should().NotContain(m => m.Id == inTheLeague.Id,
            "a division runs several competitions at once, and this filter picks one of them");
    }

    [Fact]
    public async Task Matches_WithNoDivision_ReturnEveryFixtureOfTheSeason()
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);
        var one = await AddMatchAsync(scenario.DivisionOneId, scenario.TeamAId, scenario.TeamBId);
        var two = await AddMatchAsync(scenario.DivisionTwoId, scenario.TeamCId, scenario.TeamAId);

        var result = await new GetFixturesQueryHandler(_fixture.DbContext).Handle(
            new GetFixturesQuery(Season: 2026, PageSize: 100), CancellationToken.None);

        result.Items.Should().Contain(m => m.Id == one.Id);
        result.Items.Should().Contain(m => m.Id == two.Id);
    }

    [Fact]
    public async Task Matches_NarrowToATeam_FromEitherSideOfTheFixture()
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);

        // The same club, listed first in one fixture and second in the other. A filter that
        // only looked at the home side would drop half of a team's season.
        var atHome = await AddMatchAsync(
            scenario.DivisionOneId, scenario.TeamAId, scenario.TeamBId);
        var away = await AddMatchAsync(
            scenario.DivisionOneId, scenario.TeamBId, scenario.TeamAId);
        var notInvolved = await AddMatchAsync(
            scenario.DivisionTwoId, scenario.TeamCId, scenario.TeamBId);

        var result = await new GetFixturesQueryHandler(_fixture.DbContext).Handle(
            new GetFixturesQuery(Season: 2026, TeamId: scenario.TeamAId, PageSize: 100),
            CancellationToken.None);

        result.Items.Should().Contain(m => m.Id == atHome.Id);
        result.Items.Should().Contain(m => m.Id == away.Id,
            "a team plays half its fixtures away, and those are still its matches");
        result.Items.Should().NotContain(m => m.Id == notInvolved.Id);
    }

    [Fact]
    public async Task Matches_TeamAndDivisionTogether_NarrowToThatTeam()
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);

        var teamAMatch = await AddMatchAsync(
            scenario.DivisionOneId, scenario.TeamAId, scenario.TeamBId);
        var withoutTeamA = await AddMatchAsync(
            scenario.DivisionOneId, scenario.TeamBId, scenario.TeamCId);

        var result = await new GetFixturesQueryHandler(_fixture.DbContext).Handle(
            new GetFixturesQuery(
                Season: 2026, TeamId: scenario.TeamAId,
                PageSize: 100, DivisionId: scenario.DivisionOneId),
            CancellationToken.None);

        result.Items.Should().Contain(m => m.Id == teamAMatch.Id);
        result.Items.Should().NotContain(m => m.Id == withoutTeamA.Id,
            "both fixtures are in the division, so the team filter has to narrow further");
    }

    // ── Suspensions ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Suspensions_NarrowToATeam()
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);
        var teamBPlayer = await AddPlayerAsync(scenario.TeamBId, jersey: 12);

        var suspendedInTeamA = await SuspendAsync(scenario.PlayerAId);
        var suspendedInTeamB = await SuspendAsync(teamBPlayer.Id);

        var result = await new GetActiveSuspensionsQueryHandler(_fixture.DbContext).Handle(
            new GetActiveSuspensionsQuery(TeamId: scenario.TeamAId), CancellationToken.None);

        result.Should().Contain(s => s.Id == suspendedInTeamA.Id);
        result.Should().NotContain(s => s.Id == suspendedInTeamB.Id,
            "both play in the same division, so only the team filter separates them");
    }

    [Fact]
    public async Task Suspensions_DivisionAndTeamTogether_NarrowToThatTeam()
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);
        var teamBPlayer = await AddPlayerAsync(scenario.TeamBId, jersey: 13);

        var suspendedInTeamA = await SuspendAsync(scenario.PlayerAId);
        var suspendedInTeamB = await SuspendAsync(teamBPlayer.Id);

        var result = await new GetActiveSuspensionsQueryHandler(_fixture.DbContext).Handle(
            new GetActiveSuspensionsQuery(scenario.DivisionOneId, scenario.TeamAId),
            CancellationToken.None);

        result.Should().Contain(s => s.Id == suspendedInTeamA.Id);
        result.Should().NotContain(s => s.Id == suspendedInTeamB.Id);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task<Player> AddPlayerAsync(Guid teamId, int jersey)
    {
        var player = new Player
        {
            Id = Guid.NewGuid(),
            FirstName = "Squad",
            LastName = "Member",
            TeamId = teamId,
            JerseyNumber = jersey,
            Position = PlayerPosition.CM,
            PreferredFoot = PreferredFoot.Right,
            DateOfBirth = new DateTime(1999, 4, 4, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        _fixture.DbContext.Players.Add(player);
        await _fixture.DbContext.SaveChangesAsync();
        return player;
    }

    /// <summary>
    /// A fixture in the shape production actually holds: belonging to a competition, which
    /// belongs to a division. A fixture with no competition cannot exist once the migration
    /// has run, so seeding one here would have tested a row the app can no longer produce.
    /// </summary>
    private async Task<MatchResult> AddMatchAsync(
        Guid divisionId, Guid homeTeamId, Guid awayTeamId, Guid? competitionId = null)
    {
        var match = new MatchResult
        {
            Id = Guid.NewGuid(),
            CompetitionId = competitionId ?? await TheLeagueOfAsync(divisionId),
            HomeTeamId = homeTeamId,
            AwayTeamId = awayTeamId,
            Season = 2026,
            MatchweekNumber = 1,
            MatchDate = new DateTime(2026, 4, 1, 15, 0, 0, DateTimeKind.Utc),
            Status = MatchStatus.Scheduled,
            CreatedAt = DateTime.UtcNow,
        };

        _fixture.DbContext.MatchResults.Add(match);
        await _fixture.DbContext.SaveChangesAsync();
        return match;
    }

    /// <summary>The division's league, made once and reused — what its fixtures belong to.</summary>
    private async Task<Guid> TheLeagueOfAsync(Guid divisionId)
    {
        var existing = await _fixture.DbContext.Competitions
            .Where(c => c.Format == CompetitionFormat.League
                        && c.Entries.Any(e => e.Team.DivisionId == divisionId))
            .Select(c => c.Id)
            .FirstOrDefaultAsync();

        if (existing != Guid.Empty) return existing;

        var id = await AddCompetitionAsync(CompetitionFormat.League);

        // Entered rather than owned: this is the only thing that makes it the division's
        // league, and it is what TheLeagueOfAsync looks for next time.
        var teams = await _fixture.DbContext.Teams
            .Where(t => t.DivisionId == divisionId)
            .Select(t => t.Id)
            .ToListAsync();

        foreach (var teamId in teams)
        {
            _fixture.DbContext.CompetitionEntries.Add(new CompetitionEntry
            {
                Id = Guid.NewGuid(),
                CompetitionId = id,
                TeamId = teamId,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await _fixture.DbContext.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> AddCompetitionAsync(CompetitionFormat format)
    {
        var id = Guid.NewGuid();

        _fixture.DbContext.Competitions.Add(new Competition
        {
            Id = id,
            Name = $"{format} {TestIds.Code("N")}",
            ShortCode = TestIds.Code("AF"),
            Season = 2026,
            Format = format,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });

        await _fixture.DbContext.SaveChangesAsync();
        return id;
    }

    private async Task<Suspension> SuspendAsync(Guid playerId)
    {
        var suspension = new Suspension
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            Reason = "Two bookings",
            MatchesSuspended = 1,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(14),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        _fixture.DbContext.Suspensions.Add(suspension);
        await _fixture.DbContext.SaveChangesAsync();
        return suspension;
    }
}
