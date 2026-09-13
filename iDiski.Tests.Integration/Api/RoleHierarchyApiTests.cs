using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Application.Players.Commands;
using iDiski.Application.Teams.Commands;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
using iDiski.Tests.Integration.Common;
using Xunit;

namespace iDiski.Tests.Integration.Api;

/// <summary>
/// The role hierarchy as a signed-in admin actually meets it: a real token, a real request,
/// a real status code. The handler tests check the same rules one layer down, but only a
/// request through the pipeline proves the policy on the endpoint and the ownership check in
/// the behaviour agree with each other.
///
/// The shape being asserted, from the brief:
///   SuperAdmin    — everything, everywhere.
///   DivisionAdmin — teams and players inside their division; no creating or deleting divisions.
///   TeamAdmin     — players of their teams, and updates to their team; never creating or
///                   deleting a team, since teams are handed to them.
/// </summary>
[Collection(ApiCollection.Name)]
public class RoleHierarchyApiTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;

    private Guid _divisionId;
    private Guid _otherDivisionId;
    private Guid _teamAId;          // the team admin's team, inside the division admin's division
    private Guid _outsideTeamId;    // in the other division, outside both admins' reach

    private User _superAdmin = null!;
    private User _divisionAdmin = null!;
    private User _teamAdmin = null!;

    /// <summary>Jersey numbers are unique per team, so no two tests may pick the same one.</summary>
    private static int _jersey = 10;

    public RoleHierarchyApiTests(ApiTestFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _divisionId = Guid.NewGuid();
        _otherDivisionId = Guid.NewGuid();
        _teamAId = Guid.NewGuid();
        _outsideTeamId = Guid.NewGuid();

        await _fixture.WithDbAsync(async db =>
        {
            var now = DateTime.UtcNow;

            db.Divisions.AddRange(
                new Division
                {
                    Id = _divisionId, Name = "Hierarchy Division",
                    ShortCode = ApiTestFixture.Code("HD"), Season = 2026,
                    Gender = Gender.Male, IsActive = true, CreatedAt = now
                },
                new Division
                {
                    Id = _otherDivisionId, Name = "Other Division",
                    ShortCode = ApiTestFixture.Code("OD"), Season = 2026,
                    Gender = Gender.Male, IsActive = true, CreatedAt = now
                });

            db.Teams.AddRange(
                new Team
                {
                    Id = _teamAId, Name = "Team A", ShortCode = ApiTestFixture.Code("HA"),
                    DivisionId = _divisionId, Founded = 2020, CreatedAt = now
                },
                new Team
                {
                    Id = _outsideTeamId, Name = "Outside Team", ShortCode = ApiTestFixture.Code("HO"),
                    DivisionId = _otherDivisionId, Founded = 2020, CreatedAt = now
                });

            await db.SaveChangesAsync();
        });

        _superAdmin = await _fixture.SeedUserAsync(Role.SuperAdmin);
        _divisionAdmin = await _fixture.SeedUserAsync(Role.DivisionAdmin, divisionId: _divisionId);
        _teamAdmin = await _fixture.SeedUserAsync(Role.TeamAdmin, teamId: _teamAId);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Teams ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(Role.SuperAdmin, HttpStatusCode.Created)]
    [InlineData(Role.DivisionAdmin, HttpStatusCode.Created)]
    // Teams are assigned to a team admin, not created by one.
    [InlineData(Role.TeamAdmin, HttpStatusCode.Forbidden)]
    public async Task CreatingATeam_IsOpenToDivisionAdminsAndAbove(
        Role role, HttpStatusCode expected)
    {
        var client = await ClientFor(role);

        var response = await client.PostAsJsonAsync("/api/teams", new CreateTeamCommand(
            Name: $"New Team {Guid.NewGuid():N}",
            ShortCode: ApiTestFixture.Code("N"),
            LogoUrl: null, Founded: 2021, HomeGround: null, City: null,
            PrimaryColour: null, SecondaryColour: null,
            DivisionId: _divisionId));

        response.StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task ADivisionAdmin_CannotCreateATeamInSomeoneElsesDivision()
    {
        var client = await ClientFor(Role.DivisionAdmin);

        var response = await client.PostAsJsonAsync("/api/teams", new CreateTeamCommand(
            Name: $"Trespassing {Guid.NewGuid():N}",
            ShortCode: ApiTestFixture.Code("T"),
            LogoUrl: null, Founded: 2021, HomeGround: null, City: null,
            PrimaryColour: null, SecondaryColour: null,
            DivisionId: _otherDivisionId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "a division admin works only inside the division they were given");
    }

    [Theory]
    [InlineData(Role.SuperAdmin, HttpStatusCode.NoContent)]
    [InlineData(Role.DivisionAdmin, HttpStatusCode.NoContent)]
    // Updating their own team is how a team admin sets a crest and colours.
    [InlineData(Role.TeamAdmin, HttpStatusCode.NoContent)]
    public async Task UpdatingTheirOwnTeam_IsOpenToEveryAdminOverIt(
        Role role, HttpStatusCode expected)
    {
        var client = await ClientFor(role);

        var response = await client.PutAsJsonAsync($"/api/teams/{_teamAId}", new UpdateTeamCommand(
            Id: _teamAId,
            Name: $"Renamed by {role}",
            LogoUrl: "https://cdn.example.com/crest.png",
            Founded: 2020, HomeGround: null, City: null,
            PrimaryColour: null, SecondaryColour: null,
            DivisionId: _divisionId));

        response.StatusCode.Should().Be(expected);
    }

    [Theory]
    [InlineData(Role.DivisionAdmin)]
    [InlineData(Role.TeamAdmin)]
    public async Task NeitherAdmin_CanTouchATeamOutsideTheirScope(Role role)
    {
        var client = await ClientFor(role);

        var response = await client.PutAsJsonAsync(
            $"/api/teams/{_outsideTeamId}", new UpdateTeamCommand(
                Id: _outsideTeamId, Name: "Hijacked", LogoUrl: null,
                Founded: 2020, HomeGround: null, City: null,
                PrimaryColour: null, SecondaryColour: null,
                DivisionId: _otherDivisionId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(Role.DivisionAdmin, HttpStatusCode.NoContent)]
    // Deleting a team would take its players and history with it.
    [InlineData(Role.TeamAdmin, HttpStatusCode.Forbidden)]
    public async Task DeletingATeam_IsClosedToTeamAdmins(Role role, HttpStatusCode expected)
    {
        var doomedTeamId = Guid.NewGuid();
        await _fixture.WithDbAsync(async db =>
        {
            db.Teams.Add(new Team
            {
                Id = doomedTeamId, Name = $"Doomed {role}",
                ShortCode = ApiTestFixture.Code("DM"), DivisionId = _divisionId,
                Founded = 2020, CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        });

        var client = await ClientFor(role);

        var response = await client.DeleteAsync($"/api/teams/{doomedTeamId}");

        response.StatusCode.Should().Be(expected);
    }

    // ── Players ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(Role.SuperAdmin, HttpStatusCode.Created)]
    [InlineData(Role.DivisionAdmin, HttpStatusCode.Created)]
    [InlineData(Role.TeamAdmin, HttpStatusCode.Created)]
    public async Task EveryAdminOverATeam_CanAddAPlayerToIt(Role role, HttpStatusCode expected)
    {
        var client = await ClientFor(role);

        var response = await client.PostAsJsonAsync("/api/players", NewPlayer(_teamAId));

        response.StatusCode.Should().Be(expected);
    }

    [Theory]
    [InlineData(Role.DivisionAdmin)]
    [InlineData(Role.TeamAdmin)]
    public async Task NeitherAdmin_CanAddAPlayerToATeamOutsideTheirScope(Role role)
    {
        var client = await ClientFor(role);

        var response = await client.PostAsJsonAsync("/api/players", NewPlayer(_outsideTeamId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Divisions ─────────────────────────────────────────────────────────────

    [Theory]
    // Divisions are the league's own structure: only a super admin shapes them.
    [InlineData(Role.DivisionAdmin)]
    [InlineData(Role.TeamAdmin)]
    public async Task CreatingADivision_IsRefusedToEveryoneBelowSuperAdmin(Role role)
    {
        var client = await ClientFor(role);

        var response = await client.PostAsJsonAsync("/api/divisions", new
        {
            name = "Invented Division",
            shortCode = ApiTestFixture.Code("IV"),
            season = 2026,
            gender = nameof(Gender.Male)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(Role.DivisionAdmin)]
    [InlineData(Role.TeamAdmin)]
    public async Task DeletingADivision_IsRefusedToEveryoneBelowSuperAdmin(Role role)
    {
        var client = await ClientFor(role);

        var response = await client.DeleteAsync($"/api/divisions/{_divisionId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Creating other admins ─────────────────────────────────────────────────

    [Fact]
    public async Task ASuperAdmin_CanCreateADivisionAdmin()
    {
        var client = await ClientFor(Role.SuperAdmin);

        var response = await client.PostAsJsonAsync("/api/authentication/create-user", new
        {
            email = $"new-division-admin-{Guid.NewGuid():N}@test.com",
            password = ApiTestFixture.Password,
            firstName = "New",
            lastName = "Admin",
            roles = new[] { nameof(Role.DivisionAdmin) },
            assignedDivisionIds = new[] { _divisionId }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ADivisionAdmin_CanCreateATeamAdminForTheirOwnTeam()
    {
        var client = await ClientFor(Role.DivisionAdmin);

        var response = await client.PostAsJsonAsync("/api/authentication/create-user", new
        {
            email = $"new-team-admin-{Guid.NewGuid():N}@test.com",
            password = ApiTestFixture.Password,
            firstName = "New",
            lastName = "Admin",
            roles = new[] { nameof(Role.TeamAdmin) },
            assignedTeamIds = new[] { _teamAId }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ADivisionAdmin_CannotMintAnotherDivisionAdmin()
    {
        var client = await ClientFor(Role.DivisionAdmin);

        var response = await client.PostAsJsonAsync("/api/authentication/create-user", new
        {
            email = $"escalation-{Guid.NewGuid():N}@test.com",
            password = ApiTestFixture.Password,
            firstName = "Escalated",
            lastName = "Admin",
            roles = new[] { nameof(Role.DivisionAdmin) },
            assignedDivisionIds = new[] { _divisionId }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "an admin must not be able to grant themselves a peer");
    }

    [Fact]
    public async Task ATeamAdmin_CannotCreateUsersAtAll()
    {
        var client = await ClientFor(Role.TeamAdmin);

        var response = await client.PostAsJsonAsync("/api/authentication/create-user", new
        {
            email = $"team-admin-spawn-{Guid.NewGuid():N}@test.com",
            password = ApiTestFixture.Password,
            firstName = "Spawned",
            lastName = "Admin",
            roles = new[] { nameof(Role.TeamAdmin) },
            assignedTeamIds = new[] { _teamAId }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private Task<HttpClient> ClientFor(Role role) => _fixture.CreateClientAsAsync(role switch
    {
        Role.SuperAdmin => _superAdmin,
        Role.DivisionAdmin => _divisionAdmin,
        Role.TeamAdmin => _teamAdmin,
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    });

    private static CreatePlayerCommand NewPlayer(Guid teamId) => new(
        FirstName: "New",
        LastName: $"Player{Guid.NewGuid():N}"[..12],
        ProfileImageUrl: null,
        Bio: null,
        DateOfBirth: new DateTime(2001, 5, 14, 0, 0, 0, DateTimeKind.Utc),
        Nationality: "South Africa",
        JerseyNumber: Interlocked.Increment(ref _jersey),
        Position: PlayerPosition.ST,
        PreferredFoot: PreferredFoot.Right,
        TeamId: teamId);
}
