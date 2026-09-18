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
/// Nobody owns one. A division holds clubs; a competition is what clubs go and contest, and a
/// cup played across three divisions belongs to none of them. There is therefore nothing to
/// scope these writes to, and no honest way to let a division administrator run something
/// their clubs merely happen to be in — so setting one up is the organiser's job and these
/// endpoints are SuperAdmin-only.
///
/// That is a narrowing, and the point of pinning it: a division admin could previously create
/// and manage competitions in their own division. What they keep is their clubs — their teams,
/// their players, and the results of matches their clubs played, which MatchScopeApiTests
/// covers.
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
    private Guid _competition;

    private User _divisionAdmin = null!;
    private User _superAdmin = null!;

    public CompetitionScopeApiTests(ApiTestFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _division = await ADivisionAsync();
        _club = await AClubInAsync(_division);
        _competition = await ACompetitionAsync();

        _divisionAdmin = await _fixture.SeedUserAsync(Role.DivisionAdmin, divisionId: _division);
        _superAdmin = await _fixture.SeedUserAsync(Role.SuperAdmin);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── A division admin sets none of this up ─────────────────────────────────

    [Fact]
    public async Task ADivisionAdminCannotStartACompetition()
    {
        // Not even one their own clubs would play in: which competitions exist is not a
        // division's business, because a competition is not a division's.
        var client = await _fixture.CreateClientAsAsync(_divisionAdmin);

        var response = await client.PostAsJsonAsync("/api/competitions", NewCompetition());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ADivisionAdminCannotRenameACompetition()
    {
        var client = await _fixture.CreateClientAsAsync(_divisionAdmin);

        var response = await client.PutAsJsonAsync(
            $"/api/competitions/{_competition}",
            new
            {
                competitionId = _competition,
                name = "Renamed By An Outsider",
                shortCode = ApiTestFixture.Code("XX"),
                format = "League",
                gender = "Male",
                isActive = true,
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var after = await _fixture.WithDbAsync(db => db.Competitions
            .AsNoTracking().FirstAsync(c => c.Id == _competition));

        after.Name.Should().NotBe("Renamed By An Outsider");
    }

    [Fact]
    public async Task ADivisionAdminCannotDeleteACompetition()
    {
        var client = await _fixture.CreateClientAsAsync(_divisionAdmin);

        var response = await client.DeleteAsync($"/api/competitions/{_competition}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await _fixture.WithDbAsync(db => db.Competitions.AnyAsync(c => c.Id == _competition)))
            .Should().BeTrue();
    }

    [Fact]
    public async Task ADivisionAdminCannotEnterTheirOwnClubIntoACompetition()
    {
        // Who is in a competition is the organiser's decision. A division admin entering their
        // own club would be picking the field a side at a time.
        var client = await _fixture.CreateClientAsAsync(_divisionAdmin);

        var response = await client.PostAsync(
            $"/api/competitions/{_competition}/entrants/{_club}", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await _fixture.WithDbAsync(db => db.CompetitionEntries
            .AnyAsync(e => e.CompetitionId == _competition && e.TeamId == _club)))
            .Should().BeFalse();
    }

    [Fact]
    public async Task ADivisionAdminCannotDrawAFixture()
    {
        // Deciding who meets whom is running the competition, not administering either club.
        var client = await _fixture.CreateClientAsAsync(_divisionAdmin);
        var other = await AClubInAsync(_division);

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

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
