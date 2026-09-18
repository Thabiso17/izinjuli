using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
using iDiski.Tests.Integration.Common;
using Xunit;

namespace iDiski.Tests.Integration.Api;

/// <summary>
/// The endpoints behind the administrators page.
///
/// These existed for a long time with no screen in front of them, and nothing exercised them
/// either, which is how the detail endpoint came to return null roles for every user without
/// anyone noticing — the compiler had been warning about it all along.
/// </summary>
[Collection(ApiCollection.Name)]
public class UserManagementApiTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;

    private Guid _divisionId;
    private Guid _competitionId;
    private Guid _teamId;
    private User _superAdmin = null!;
    private User _teamAdmin = null!;

    public UserManagementApiTests(ApiTestFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _divisionId = Guid.NewGuid();
        _competitionId = Guid.NewGuid();
        _teamId = Guid.NewGuid();

        await _fixture.WithDbAsync(async db =>
        {
            var now = DateTime.UtcNow;

            db.Divisions.Add(new Division
            {
                Id = _divisionId, Name = "User Division",
                ShortCode = ApiTestFixture.Code("UD"), Season = 2026,
                Gender = Gender.Male, IsActive = true, CreatedAt = now
            });

            db.Teams.Add(new Team
            {
                Id = _teamId, Name = "User Team", ShortCode = ApiTestFixture.Code("UT"),
                DivisionId = _divisionId, Founded = 2020, CreatedAt = now
            });

            db.Competitions.Add(new Competition
            {
                Id = _competitionId, Name = "User Competition",
                ShortCode = ApiTestFixture.Code("UC"), Season = 2026,
                Format = CompetitionFormat.League, Gender = Gender.Male,
                IsActive = true, CreatedAt = now
            });

            await db.SaveChangesAsync();
        });

        _superAdmin = await _fixture.SeedUserAsync(Role.SuperAdmin);
        _teamAdmin = await _fixture.SeedUserAsync(Role.TeamAdmin, teamId: _teamId);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TheListLoads_AndSaysWhatEachAdministratorIs()
    {
        var client = await _fixture.CreateClientAsAsync(_superAdmin);

        var response = await client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var users = body.RootElement.EnumerateArray().ToList();

        users.Should().NotBeEmpty();

        var superAdmin = users.Single(u => u.GetProperty("id").GetGuid() == _superAdmin.Id);
        var roleIds = superAdmin.GetProperty("roleIds").EnumerateArray().Select(r => r.GetInt32());

        // A list of administrators that does not say what anyone administers cannot be acted on.
        roleIds.Should().Contain((int)Role.SuperAdmin);
    }

    [Fact]
    public async Task TheDetailEndpoint_ReturnsRolesRatherThanNull()
    {
        var client = await _fixture.CreateClientAsAsync(_superAdmin);

        var response = await client.GetAsync($"/api/users/{_teamAdmin.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var user = body.RootElement;

        // This came back null for every user: the projection cast a List<Role> with
        // `as IReadOnlyList<int>`, which is never that type, so the result was always null.
        user.GetProperty("roleIds").ValueKind.Should().Be(JsonValueKind.Array);
        user.GetProperty("roleIds").EnumerateArray().Select(r => r.GetInt32())
            .Should().Contain((int)Role.TeamAdmin);

        user.GetProperty("assignedTeamIds").EnumerateArray().Select(t => t.GetGuid())
            .Should().Contain(_teamId);
    }

    [Fact]
    public async Task ARoleCanBeGrantedAndTakenAway()
    {
        var client = await _fixture.CreateClientAsAsync(_superAdmin);
        var user = await _fixture.SeedUserAsync(Role.TeamAdmin, teamId: _teamId);

        var granted = await client.PostAsJsonAsync(
            $"/api/users/{user.Id}/roles",
            new { userId = user.Id, role = (int)Role.CompetitionAdmin });
        granted.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        (await RoleIdsOf(client, user.Id)).Should().Contain((int)Role.CompetitionAdmin);

        var removed = await client.DeleteAsync(
            $"/api/users/{user.Id}/roles/{(int)Role.CompetitionAdmin}");
        removed.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        (await RoleIdsOf(client, user.Id)).Should().NotContain((int)Role.CompetitionAdmin);
    }

    [Fact]
    public async Task ADivisionCanBeAssignedAndRemoved()
    {
        var client = await _fixture.CreateClientAsAsync(_superAdmin);
        var user = await _fixture.SeedUserAsync(Role.CompetitionAdmin, competitionId: _competitionId);

        var other = Guid.NewGuid();
        await _fixture.WithDbAsync(async db =>
        {
            db.Divisions.Add(new Division
            {
                Id = other, Name = "Second Division", ShortCode = ApiTestFixture.Code("SD"),
                Season = 2026, Gender = Gender.Male, IsActive = true, CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        });

        var assigned = await client.PostAsJsonAsync(
            $"/api/users/{user.Id}/divisions", new { userId = user.Id, divisionId = other });
        assigned.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        (await DivisionIdsOf(client, user.Id)).Should().Contain(other);

        var removed = await client.DeleteAsync($"/api/users/{user.Id}/divisions/{other}");
        removed.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        (await DivisionIdsOf(client, user.Id)).Should().NotContain(other);
    }

    [Fact]
    public async Task DeactivatingAnAdministrator_StopsThemSigningIn()
    {
        var client = await _fixture.CreateClientAsAsync(_superAdmin);
        var user = await _fixture.SeedUserAsync(Role.TeamAdmin, teamId: _teamId);

        var update = await client.PutAsJsonAsync($"/api/users/{user.Id}", new
        {
            id = user.Id,
            firstName = user.FirstName,
            lastName = user.LastName,
            isActive = false,
        });
        update.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        // The point of deactivating somebody, rather than a flag that only changes a badge.
        var anonymous = _fixture.CreateClient();
        var login = await anonymous.PostAsJsonAsync("/api/auth/login", new
        {
            email = user.Email,
            password = ApiTestFixture.Password,
        });

        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnAdministratorBelowSuperAdmin_CannotReadTheList()
    {
        var client = await _fixture.CreateClientAsAsync(_teamAdmin);

        var response = await client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "who else has access, and to what, is not a team admin's to see");
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static async Task<int[]> RoleIdsOf(HttpClient client, Guid userId)
    {
        var response = await client.GetAsync($"/api/users/{userId}");
        response.EnsureSuccessStatusCode();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("roleIds")
            .EnumerateArray().Select(r => r.GetInt32()).ToArray();
    }

    private static async Task<Guid[]> DivisionIdsOf(HttpClient client, Guid userId)
    {
        var response = await client.GetAsync($"/api/users/{userId}");
        response.EnsureSuccessStatusCode();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("assignedCompetitionIds")
            .EnumerateArray().Select(d => d.GetGuid()).ToArray();
    }
}
