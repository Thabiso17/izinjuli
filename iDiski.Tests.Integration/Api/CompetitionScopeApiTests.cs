using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
using iDiski.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace iDiski.Tests.Integration.Api;

/// <summary>
/// Who may run a competition.
///
/// Whoever was assigned to it. A competition admin holds a list of the competitions they run,
/// and that list is the whole of their reach: everything about running one — the entry list,
/// the draw, the results — is theirs, and the competition next to it is not.
///
/// Scoping is deliberately not "one of the entrants is mine": a cup drawing clubs from three
/// divisions would otherwise answer to three sets of administrators, none of whom was asked to
/// run it. Creating a competition stays with a SuperAdmin, who then says who runs it.
///
/// Over HTTP rather than against a handler, because the policy is what does the refusing and
/// calling a handler directly would skip it.
/// </summary>
[Collection(ApiCollection.Name)]
public class CompetitionScopeApiTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;

    private Guid _division;
    private Guid _club;

    /// <summary>The one they were given.</summary>
    private Guid _competition;

    /// <summary>One they were not.</summary>
    private Guid _somebodyElses;

    private User _competitionAdmin = null!;
    private User _superAdmin = null!;

    public CompetitionScopeApiTests(ApiTestFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _division = await ADivisionAsync();
        _club = await AClubInAsync(_division);
        _competition = await ACompetitionAsync();
        _somebodyElses = await ACompetitionAsync();

        _competitionAdmin = await _fixture.SeedUserAsync(
            Role.CompetitionAdmin, competitionId: _competition);
        _superAdmin = await _fixture.SeedUserAsync(Role.SuperAdmin);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Their own ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ACompetitionAdminRunsTheOneTheyWereGiven()
    {
        var client = await _fixture.CreateClientAsAsync(_competitionAdmin);

        var response = await client.PostAsync(
            $"/api/competitions/{_competition}/entrants/{_club}", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await _fixture.WithDbAsync(db => db.CompetitionEntries
            .AnyAsync(e => e.CompetitionId == _competition && e.TeamId == _club)))
            .Should().BeTrue();
    }

    [Fact]
    public async Task ACompetitionAdminRenamesTheirOwn()
    {
        var client = await _fixture.CreateClientAsAsync(_competitionAdmin);

        var response = await client.PutAsJsonAsync(
            $"/api/competitions/{_competition}", Renamed(_competition, "Renamed By Its Organiser"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await _fixture.WithDbAsync(db => db.Competitions
            .AsNoTracking().FirstAsync(c => c.Id == _competition));

        after.Name.Should().Be("Renamed By Its Organiser");
    }

    [Fact]
    public async Task ACompetitionAdminDrawsAFixtureInTheirOwn()
    {
        // Deciding who meets whom is running the competition, and running it is what they
        // were assigned.
        var other = await AClubInAsync(_division);
        var client = await _fixture.CreateClientAsAsync(_competitionAdmin);

        foreach (var club in new[] { _club, other })
        {
            (await client.PostAsync(
                $"/api/competitions/{_competition}/entrants/{club}", content: null))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        var response = await client.PostAsJsonAsync("/api/matchresults", new
        {
            competitionId = _competition,
            matchDate = new DateTime(2043, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            matchweekNumber = 1,
            homeTeamId = _club,
            awayTeamId = other,
            venue = (string?)null,
            referee = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── Somebody else's ───────────────────────────────────────────────────────

    [Fact]
    public async Task ACompetitionAdminCannotTouchACompetitionTheyWereNotGiven()
    {
        // Holding the role is not the point; holding this competition is. The policy on the
        // endpoint lets them through and the ownership check is what stops them.
        var client = await _fixture.CreateClientAsAsync(_competitionAdmin);

        var response = await client.PostAsync(
            $"/api/competitions/{_somebodyElses}/entrants/{_club}", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await _fixture.WithDbAsync(db => db.CompetitionEntries
            .AnyAsync(e => e.CompetitionId == _somebodyElses && e.TeamId == _club)))
            .Should().BeFalse();
    }

    [Fact]
    public async Task ACompetitionAdminCannotRenameSomebodyElsesCompetition()
    {
        var client = await _fixture.CreateClientAsAsync(_competitionAdmin);

        var response = await client.PutAsJsonAsync(
            $"/api/competitions/{_somebodyElses}", Renamed(_somebodyElses, "Renamed By An Outsider"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var after = await _fixture.WithDbAsync(db => db.Competitions
            .AsNoTracking().FirstAsync(c => c.Id == _somebodyElses));

        after.Name.Should().NotBe("Renamed By An Outsider");
    }

    [Fact]
    public async Task ACompetitionAdminCannotDrawAFixtureIntoSomebodyElsesCompetition()
    {
        var client = await _fixture.CreateClientAsAsync(_competitionAdmin);
        var other = await AClubInAsync(_division);

        var response = await client.PostAsJsonAsync("/api/matchresults", new
        {
            competitionId = _somebodyElses,
            matchDate = new DateTime(2043, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            matchweekNumber = 1,
            homeTeamId = _club,
            awayTeamId = other,
            venue = (string?)null,
            referee = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ACompetitionAdminCannotStartANewCompetition()
    {
        // A SuperAdmin decides what gets played and who runs it. Otherwise the role could
        // grant itself a competition and then administer it.
        var client = await _fixture.CreateClientAsAsync(_competitionAdmin);

        var response = await client.PostAsJsonAsync("/api/competitions", NewCompetition());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ACompetitionAdminCannotEditAClubInTheirOwnCompetition()
    {
        // The line that moved: running a cup a club is entered in is no reason to be able to
        // rename the club, which is in three other competitions and belongs to none of them.
        var client = await _fixture.CreateClientAsAsync(_competitionAdmin);

        var response = await client.PutAsJsonAsync($"/api/teams/{_club}", new
        {
            id = _club,
            name = "Renamed By A Competition Admin",
            shortCode = ApiTestFixture.Code("RN"),
            founded = 2020,
        });

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);

        var after = await _fixture.WithDbAsync(db => db.Teams
            .AsNoTracking().FirstAsync(t => t.Id == _club));

        after.Name.Should().NotBe("Renamed By A Competition Admin");
    }

    // ── The organiser does ────────────────────────────────────────────────────

    [Fact]
    public async Task ASuperAdminStartsOne()
    {
        var client = await _fixture.CreateClientAsAsync(_superAdmin);

        var response = await client.PostAsJsonAsync("/api/competitions", NewCompetition());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ASuperAdminEntersClubsFromAnyDivision()
    {
        // The Nedbank case over the wire: a competition belonging to nothing, taking clubs
        // from wherever the organiser wants them.
        var elsewhere = await ADivisionAsync();
        var guest = await AClubInAsync(elsewhere);

        var client = await _fixture.CreateClientAsAsync(_superAdmin);

        foreach (var club in new[] { _club, guest })
        {
            var response = await client.PostAsync(
                $"/api/competitions/{_competition}/entrants/{club}", content: null);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        var entered = await _fixture.WithDbAsync(db => db.CompetitionEntries
            .CountAsync(e => e.CompetitionId == _competition));

        entered.Should().Be(2);
    }

    [Fact]
    public async Task AClubFromAWomensDivisionIsRefusedByTheCompetitionsOwnGender()
    {
        // The rule that had to move when the division it used to be read from went away.
        var womens = await ADivisionAsync(Gender.Female);
        var womensClub = await AClubInAsync(womens);

        var client = await _fixture.CreateClientAsAsync(_superAdmin);

        var response = await client.PostAsync(
            $"/api/competitions/{_competition}/entrants/{womensClub}", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        (await _fixture.WithDbAsync(db => db.CompetitionEntries
            .AnyAsync(e => e.CompetitionId == _competition && e.TeamId == womensClub)))
            .Should().BeFalse();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static object Renamed(Guid id, string name) => new
    {
        competitionId = id,
        name,
        shortCode = ApiTestFixture.Code("XX"),
        format = "Knockout",
        gender = "Male",
        isActive = true,
    };

    private static object NewCompetition() => new
    {
        name = $"Competition {ApiTestFixture.Code("N")}",
        shortCode = ApiTestFixture.Code("CS"),
        season = 2043,
        format = "Knockout",
        gender = "Male",
        enterTeamsFromDivisionId = (Guid?)null,
    };

    private Task<Guid> ADivisionAsync(Gender gender = Gender.Male) =>
        _fixture.WithDbAsync(async db =>
        {
            var id = Guid.NewGuid();

            db.Divisions.Add(new Division
            {
                Id = id,
                Name = $"Division {ApiTestFixture.Code("N")}",
                ShortCode = ApiTestFixture.Code("CS"),
                Season = 2043,
                Gender = gender,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });

            await db.SaveChangesAsync();
            return id;
        });

    private Task<Guid> AClubInAsync(Guid divisionId) => _fixture.WithDbAsync(async db =>
    {
        var id = Guid.NewGuid();

        db.Teams.Add(new Team
        {
            Id = id,
            Name = $"Club {ApiTestFixture.Code("N")}",
            ShortCode = ApiTestFixture.Code("C"),
            DivisionId = divisionId,
            Founded = 2020,
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
        return id;
    });

    private Task<Guid> ACompetitionAsync() => _fixture.WithDbAsync(async db =>
    {
        var id = Guid.NewGuid();

        db.Competitions.Add(new Competition
        {
            Id = id,
            Name = $"Competition {ApiTestFixture.Code("N")}",
            ShortCode = ApiTestFixture.Code("CX"),
            Season = 2043,
            Format = CompetitionFormat.Knockout,
            Gender = Gender.Male,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
        return id;
    });
}
