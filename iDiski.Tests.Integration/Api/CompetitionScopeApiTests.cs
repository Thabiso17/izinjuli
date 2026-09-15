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
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace iDiski.Tests.Integration.Api;

/// <summary>
/// Who may run a competition.
///
/// Every write here is guarded by <c>CanManageDivisions</c>, which asks only whether somebody
/// is a division admin at all — never which divisions. The scoping comes from
/// <c>IRequireCompetitionAccess</c>, which resolves a competition to the division running it,
/// and it is worth pinning because the obvious alternative is wrong: scoping by the entrants
/// would hand a sponsor's cup to the administrators of every division whose clubs were
/// invited into it.
///
/// Over HTTP rather than against a handler, because the thing being tested is the pipeline —
/// calling a handler directly skips the behaviour that does the checking, and would pass just
/// as happily with the hole still open.
/// </summary>
[Collection(ApiCollection.Name)]
public class CompetitionScopeApiTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;

    private Guid _ownDivision;
    private Guid _otherDivision;
    private Guid _ownCompetition;
    private Guid _otherCompetition;
    private Guid _ownClub;
    private Guid _otherClub;

    private User _divisionAdmin = null!;
    private User _superAdmin = null!;

    public CompetitionScopeApiTests(ApiTestFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _ownDivision = await ADivisionAsync();
        _otherDivision = await ADivisionAsync();

        _ownClub = await AClubInAsync(_ownDivision);
        _otherClub = await AClubInAsync(_otherDivision);

        _ownCompetition = await ACompetitionInAsync(_ownDivision);
        _otherCompetition = await ACompetitionInAsync(_otherDivision);

        _divisionAdmin = await _fixture.SeedUserAsync(Role.DivisionAdmin, divisionId: _ownDivision);
        _superAdmin = await _fixture.SeedUserAsync(Role.SuperAdmin);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Starting one ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ADivisionAdminCannotStartACompetitionInSomebodyElsesDivision()
    {
        var client = await _fixture.CreateClientAsAsync(_divisionAdmin);

        var response = await client.PostAsJsonAsync("/api/competitions", NewCompetitionIn(_otherDivision));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var started = await _fixture.WithDbAsync(db => db.Competitions
            .CountAsync(c => c.DivisionId == _otherDivision));

        started.Should().Be(1, "nothing was written on the way to being refused");
    }

    [Fact]
    public async Task ADivisionAdminCanStartACompetitionInTheirOwn()
    {
        // The half that stops this being a fix which simply refuses everybody.
        var client = await _fixture.CreateClientAsAsync(_divisionAdmin);

        var response = await client.PostAsJsonAsync("/api/competitions", NewCompetitionIn(_ownDivision));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── Changing and abandoning one ───────────────────────────────────────────

    [Fact]
    public async Task ADivisionAdminCannotRenameSomebodyElsesCompetition()
    {
        var client = await _fixture.CreateClientAsAsync(_divisionAdmin);

        var response = await client.PutAsJsonAsync(
            $"/api/competitions/{_otherCompetition}",
            new
            {
                competitionId = _otherCompetition,
                name = "Renamed By An Outsider",
                shortCode = ApiTestFixture.Code("XX"),
                format = "League",
                isActive = true,
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var after = await _fixture.WithDbAsync(db => db.Competitions
            .AsNoTracking().FirstAsync(c => c.Id == _otherCompetition));

        after.Name.Should().NotBe("Renamed By An Outsider");
    }

    [Fact]
    public async Task ADivisionAdminCannotDeleteSomebodyElsesCompetition()
    {
        var client = await _fixture.CreateClientAsAsync(_divisionAdmin);

        var response = await client.DeleteAsync($"/api/competitions/{_otherCompetition}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await _fixture.WithDbAsync(db => db.Competitions.AnyAsync(c => c.Id == _otherCompetition)))
            .Should().BeTrue();
    }

    // ── The entry list ────────────────────────────────────────────────────────

    [Fact]
    public async Task ADivisionAdminCannotEnterAClubIntoSomebodyElsesCompetition()
    {
        // Not even one of their own clubs: who plays in a cup is the organiser's decision,
        // not something another division's administrator can help themselves to.
        var client = await _fixture.CreateClientAsAsync(_divisionAdmin);

        var response = await client.PostAsync(
            $"/api/competitions/{_otherCompetition}/entrants/{_ownClub}", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await _fixture.WithDbAsync(db => db.CompetitionEntries
            .AnyAsync(e => e.CompetitionId == _otherCompetition && e.TeamId == _ownClub)))
            .Should().BeFalse();
    }

    [Fact]
    public async Task ADivisionAdminMayInviteAnOutsideClubIntoTheirOwnCompetition()
    {
        // The Nedbank Cup case, and the reason scoping by entrants would have been wrong: the
        // invited club is somebody else's, and the competition stays the host's to run.
        var client = await _fixture.CreateClientAsAsync(_divisionAdmin);

        var response = await client.PostAsync(
            $"/api/competitions/{_ownCompetition}/entrants/{_otherClub}", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await _fixture.WithDbAsync(db => db.CompetitionEntries
            .AnyAsync(e => e.CompetitionId == _ownCompetition && e.TeamId == _otherClub)))
            .Should().BeTrue();
    }

    [Fact]
    public async Task ADivisionAdminCannotWithdrawAClubFromSomebodyElsesCompetition()
    {
        await _fixture.WithDbAsync(async db =>
        {
            db.CompetitionEntries.Add(new CompetitionEntry
            {
                Id = Guid.NewGuid(),
                CompetitionId = _otherCompetition,
                TeamId = _otherClub,
                CreatedAt = DateTime.UtcNow,
            });

            await db.SaveChangesAsync();
        });

        var client = await _fixture.CreateClientAsAsync(_divisionAdmin);

        var response = await client.DeleteAsync(
            $"/api/competitions/{_otherCompetition}/entrants/{_otherClub}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await _fixture.WithDbAsync(db => db.CompetitionEntries
            .AnyAsync(e => e.CompetitionId == _otherCompetition && e.TeamId == _otherClub)))
            .Should().BeTrue("a refused withdrawal must leave the entry where it was");
    }

    [Fact]
    public async Task ASuperAdminIsUnaffected()
    {
        // SuperAdmin passes every ownership check by design, and must keep doing so — they are
        // the only person who can put right a competition nobody else's scope covers.
        var client = await _fixture.CreateClientAsAsync(_superAdmin);

        var response = await client.PostAsJsonAsync("/api/competitions", NewCompetitionIn(_otherDivision));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static object NewCompetitionIn(Guid divisionId) => new
    {
        divisionId,
        name = $"Competition {ApiTestFixture.Code("N")}",
        shortCode = ApiTestFixture.Code("CS"),
        season = 2042,
        format = "Knockout",
        enterAllDivisionTeams = false,
    };

    private Task<Guid> ADivisionAsync() => _fixture.WithDbAsync(async db =>
    {
        var id = Guid.NewGuid();

        db.Divisions.Add(new Division
        {
            Id = id,
            Name = $"Division {ApiTestFixture.Code("N")}",
            ShortCode = ApiTestFixture.Code("CS"),
            Season = 2042,
            Gender = Gender.Male,
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

    private Task<Guid> ACompetitionInAsync(Guid divisionId) => _fixture.WithDbAsync(async db =>
    {
        var id = Guid.NewGuid();

        db.Competitions.Add(new Competition
        {
            Id = id,
            DivisionId = divisionId,
            Name = $"Competition {ApiTestFixture.Code("N")}",
            ShortCode = ApiTestFixture.Code("CX"),
            Season = 2042,
            Format = CompetitionFormat.Knockout,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
        return id;
    });
}
