using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
using iDiski.Tests.Integration.Common;
using Xunit;

namespace iDiski.Tests.Integration.Api;

/// <summary>
/// What a competition admin may write outside the competitions they run, and what they may not.
///
/// The endpoints are guarded by <c>CanManageCompetitions</c>, which asks only whether somebody
/// holds the role — never which competitions. What narrows it is the ownership behaviour, and
/// the writes that matter are: entering a result, which moves a table and in a knockout puts a
/// club into the next round; drawing a new fixture; and recording the goals and cards behind a
/// result. Suspending a player is not among them — that belongs to the club's own admins.
///
/// Over HTTP rather than against a handler, because the thing being tested is the pipeline —
/// calling a handler directly skips the behaviour that does the checking, and would pass just
/// as happily with the hole still open.
/// </summary>
[Collection(ApiCollection.Name)]
public class MatchScopeApiTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;

    private Guid _ownDivision;
    private Guid _otherDivision;
    private (Guid Home, Guid Away) _ownClubs;
    private (Guid Home, Guid Away) _otherClubs;
    private Guid _ownCompetition;
    private Guid _otherCompetition;
    private Guid _ownFixture;
    private Guid _otherFixture;
    private Guid _ownPlayer;
    private Guid _otherPlayer;

    private User _competitionAdmin = null!;
    private User _superAdmin = null!;

    public MatchScopeApiTests(ApiTestFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _ownDivision = await ADivisionAsync();
        _otherDivision = await ADivisionAsync();

        _ownClubs = await TwoClubsInAsync(_ownDivision);
        _otherClubs = await TwoClubsInAsync(_otherDivision);

        _ownCompetition = await ACompetitionForAsync(_ownClubs);
        _otherCompetition = await ACompetitionForAsync(_otherClubs);

        _ownFixture = await AFixtureInAsync(_ownCompetition, _ownClubs);
        _otherFixture = await AFixtureInAsync(_otherCompetition, _otherClubs);

        _ownPlayer = await APlayerInAsync(_ownClubs.Home);
        _otherPlayer = await APlayerInAsync(_otherClubs.Home);

        _competitionAdmin = await _fixture.SeedUserAsync(
            Role.CompetitionAdmin, competitionId: _ownCompetition);
        _superAdmin = await _fixture.SeedUserAsync(Role.SuperAdmin);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ACompetitionAdminCannotScoreAMatchInACompetitionTheyDoNotRun()
    {
        var client = await _fixture.CreateClientAsAsync(_competitionAdmin);

        var response = await client.PutAsJsonAsync(
            $"/api/matchresults/{_otherFixture}/score",
            ScoreOf(_otherFixture, 9, 0));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // And nothing was written on the way to being refused.
        var after = await _fixture.WithDbAsync(db => db.MatchResults.FindAsync(_otherFixture).AsTask());
        after!.HomeScore.Should().Be(0);
        after.Status.Should().Be(MatchStatus.Scheduled);
    }

    [Fact]
    public async Task ACompetitionAdminScoresAMatchInTheirOwnCompetition()
    {
        // The half that stops this being a fix that simply refuses everybody.
        var client = await _fixture.CreateClientAsAsync(_competitionAdmin);

        var response = await client.PutAsJsonAsync(
            $"/api/matchresults/{_ownFixture}/score",
            ScoreOf(_ownFixture, 2, 1));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await _fixture.WithDbAsync(db => db.MatchResults.FindAsync(_ownFixture).AsTask());
        after!.HomeScore.Should().Be(2);
        after.Status.Should().Be(MatchStatus.Completed);
    }

    [Fact]
    public async Task ACompetitionAdminCannotDrawAFixtureIntoAnothersCompetition()
    {
        var client = await _fixture.CreateClientAsAsync(_competitionAdmin);

        var response = await client.PostAsJsonAsync("/api/matchresults", new
        {
            competitionId = _otherCompetition,
            matchDate = new DateTime(2041, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            matchweekNumber = 2,
            homeTeamId = _otherClubs.Home,
            awayTeamId = _otherClubs.Away,
            venue = (string?)null,
            referee = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ASuperAdminIsUnaffected()
    {
        // SuperAdmin passes every ownership check by design, and must keep doing so — they are
        // the only person who can put right a fixture nobody else's scope covers.
        var client = await _fixture.CreateClientAsAsync(_superAdmin);

        var response = await client.PutAsJsonAsync(
            $"/api/matchresults/{_otherFixture}/score",
            ScoreOf(_otherFixture, 3, 3));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ACompetitionAdminCannotRecordGoalsInAnothersCompetition()
    {
        // Scoping the score and leaving the goals open would have handed most of it straight
        // back: the events are the detail the score is made of.
        var client = await _fixture.CreateClientAsAsync(_competitionAdmin);

        var response = await client.PostAsJsonAsync("/api/matchevents", new
        {
            matchId = _otherFixture,
            events = new[]
            {
                new { playerId = _otherPlayer, eventType = "Goal", minute = 12, additionalInfo = (string?)null },
            },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ACompetitionAdminCannotSuspendAPlayerAtAll()
    {
        // This one moved rather than narrowed. A suspension is about a player, and a player
        // belongs to a club — so it is the club's administrators who hand one out, not the
        // organiser of a competition the club happens to be entered in.
        var client = await _fixture.CreateClientAsAsync(_competitionAdmin);

        var response = await client.PostAsJsonAsync("/api/suspensions", new
        {
            playerId = _ownPlayer,
            reason = "A ban that is not an organiser's to hand out",
            matchesSuspended = 3,
            startDate = (DateTime?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ATeamAdminSuspendsTheirOwnPlayer()
    {
        // The other half: somebody still has to be able to do it.
        var teamAdmin = await _fixture.SeedUserAsync(Role.TeamAdmin, teamId: _ownClubs.Home);
        var client = await _fixture.CreateClientAsAsync(teamAdmin);

        var response = await client.PostAsJsonAsync("/api/suspensions", new
        {
            playerId = _ownPlayer,
            reason = "Two footed challenge",
            matchesSuspended = 2,
            startDate = (DateTime?)null,
        });

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ATeamAdminCannotSuspendSomebodyElsesPlayer()
    {
        var teamAdmin = await _fixture.SeedUserAsync(Role.TeamAdmin, teamId: _ownClubs.Home);
        var client = await _fixture.CreateClientAsAsync(teamAdmin);

        var response = await client.PostAsJsonAsync("/api/suspensions", new
        {
            playerId = _otherPlayer,
            reason = "A ban on a club that is not theirs",
            matchesSuspended = 3,
            startDate = (DateTime?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static object ScoreOf(Guid id, int home, int away) => new
    {
        id,
        homeScore = home,
        awayScore = away,
        status = "Completed",
        notes = (string?)null,
    };

    private Task<Guid> ADivisionAsync() => _fixture.WithDbAsync(async db =>
    {
        var id = Guid.NewGuid();

        db.Divisions.Add(new Division
        {
            Id = id,
            Name = $"Division {ApiTestFixture.Code("N")}",
            ShortCode = ApiTestFixture.Code("MS"),
            Season = 2041,
            Gender = Gender.Male,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
        return id;
    });

    private Task<(Guid Home, Guid Away)> TwoClubsInAsync(Guid divisionId) =>
        _fixture.WithDbAsync(async db =>
        {
            var ids = new List<Guid>();

            for (var i = 0; i < 2; i++)
            {
                var id = Guid.NewGuid();
                ids.Add(id);

                db.Teams.Add(new Team
                {
                    Id = id,
                    Name = $"Club {ApiTestFixture.Code("N")}",
                    ShortCode = ApiTestFixture.Code("M"),
                    DivisionId = divisionId,
                    Founded = 2020,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            await db.SaveChangesAsync();
            return (ids[0], ids[1]);
        });

    private Task<Guid> APlayerInAsync(Guid teamId) => _fixture.WithDbAsync(async db =>
    {
        var id = Guid.NewGuid();

        db.Players.Add(new Player
        {
            Id = id,
            FirstName = "Test",
            LastName = $"Player {ApiTestFixture.Code("N")}",
            DateOfBirth = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            // Unique per team, and every player here is on a team of their own.
            JerseyNumber = 9,
            Position = PlayerPosition.ST,
            TeamId = teamId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
        return id;
    });

    private Task<Guid> ACompetitionForAsync((Guid Home, Guid Away) clubs) =>
        _fixture.WithDbAsync(async db =>
        {
            var id = Guid.NewGuid();

            db.Competitions.Add(new Competition
            {
                Id = id,
                Name = $"Competition {ApiTestFixture.Code("N")}",
                ShortCode = ApiTestFixture.Code("MC"),
                Season = 2041,
                Format = CompetitionFormat.League,
                Gender = Gender.Male,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });

            foreach (var teamId in new[] { clubs.Home, clubs.Away })
            {
                db.CompetitionEntries.Add(new CompetitionEntry
                {
                    Id = Guid.NewGuid(),
                    CompetitionId = id,
                    TeamId = teamId,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            await db.SaveChangesAsync();
            return id;
        });

    private Task<Guid> AFixtureInAsync(Guid competitionId, (Guid Home, Guid Away) clubs) =>
        _fixture.WithDbAsync(async db =>
        {
            var id = Guid.NewGuid();

            db.MatchResults.Add(new MatchResult
            {
                Id = id,
                CompetitionId = competitionId,
                HomeTeamId = clubs.Home,
                AwayTeamId = clubs.Away,
                MatchDate = new DateTime(2041, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                MatchweekNumber = 1,
                Season = 2041,
                Status = MatchStatus.Scheduled,
                Stage = MatchStage.League,
                CreatedAt = DateTime.UtcNow,
            });

            await db.SaveChangesAsync();
            return id;
        });
}
