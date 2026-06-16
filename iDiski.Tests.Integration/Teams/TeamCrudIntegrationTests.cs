using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Application.Teams.Commands;
using iDiski.Application.Teams.Queries;
using iDiski.Tests.Integration.Common;
using Xunit;

namespace iDiski.Tests.Integration.Teams;

public class TeamCrudIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public TeamCrudIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<string> GetSuperAdminTokenAsync()
    {
        var response = await _fixture.HttpClient.PostAsJsonAsync(
            "/api/authentication/login",
            new { email = "superadmin@test.com", password = "Password123!" }
        );

        var result = await response.Content.ReadAsAsync<dynamic>();
        return result.accessToken;
    }

    private async Task<string> GetTeamAdminTokenAsync()
    {
        var response = await _fixture.HttpClient.PostAsJsonAsync(
            "/api/authentication/login",
            new { email = "teamadmin@test.com", password = "Password123!" }
        );

        var result = await response.Content.ReadAsAsync<dynamic>();
        return result.accessToken;
    }

    [Fact]
    public async Task GetTeams_ReturnsAllTeams_WithoutAuthentication()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);
        await TestDataSeeder.SeedTestDivisionsAndTeamsAsync(_fixture.DbContext);

        // Act
        var response = await _fixture.HttpClient.GetAsync("/api/teams");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var teams = await response.Content.ReadAsAsync<dynamic>();
        ((object[])teams).Length.Should().Be(2);
    }

    [Fact]
    public async Task UpdateTeam_AsSuperAdmin_Succeeds()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);
        await TestDataSeeder.SeedTestDivisionsAndTeamsAsync(_fixture.DbContext);

        var token = await GetSuperAdminTokenAsync();
        _fixture.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var teams = await _fixture.DbContext.Teams.ToListAsync();
        var teamId = teams.First().Id;

        var command = new UpdateTeamCommand(
            Id: teamId,
            Name: "Updated Team A",
            LogoUrl: null,
            Founded: 2020,
            HomeGround: "New Stadium",
            City: "New City",
            PrimaryColour: "#FF0000",
            SecondaryColour: "#FFFFFF"
        );

        // Act
        var response = await _fixture.HttpClient.PutAsJsonAsync(
            $"/api/teams/{teamId}",
            command
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedTeam = await _fixture.DbContext.Teams.FindAsync(teamId);
        updatedTeam?.Name.Should().Be("Updated Team A");
        updatedTeam?.HomeGround.Should().Be("New Stadium");
    }

    [Fact]
    public async Task UpdateTeam_AsTeamAdmin_OnAssignedTeam_Succeeds()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);
        await TestDataSeeder.SeedTestDivisionsAndTeamsAsync(_fixture.DbContext);

        var token = await GetTeamAdminTokenAsync();
        _fixture.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var teams = await _fixture.DbContext.Teams.ToListAsync();
        var assignedTeamId = teams.First().Id; // First team is assigned to TeamAdmin

        var command = new UpdateTeamCommand(
            Id: assignedTeamId,
            Name: "Team A Updated",
            LogoUrl: null,
            Founded: 2020,
            HomeGround: "Updated Stadium",
            City: null,
            PrimaryColour: null,
            SecondaryColour: null
        );

        // Act
        var response = await _fixture.HttpClient.PutAsJsonAsync(
            $"/api/teams/{assignedTeamId}",
            command
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedTeam = await _fixture.DbContext.Teams.FindAsync(assignedTeamId);
        updatedTeam?.Name.Should().Be("Team A Updated");
    }

    [Fact]
    public async Task UpdateTeam_AsTeamAdmin_OnUnassignedTeam_Returns403()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);
        await TestDataSeeder.SeedTestDivisionsAndTeamsAsync(_fixture.DbContext);

        var token = await GetTeamAdminTokenAsync();
        _fixture.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var teams = await _fixture.DbContext.Teams.ToListAsync();
        var unassignedTeamId = teams.Last().Id; // Second team is NOT assigned to TeamAdmin

        var command = new UpdateTeamCommand(
            Id: unassignedTeamId,
            Name: "Unauthorized Update",
            LogoUrl: null,
            Founded: 2020,
            HomeGround: null,
            City: null,
            PrimaryColour: null,
            SecondaryColour: null
        );

        // Act
        var response = await _fixture.HttpClient.PutAsJsonAsync(
            $"/api/teams/{unassignedTeamId}",
            command
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateTeam_WithoutAuthentication_Returns401()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);
        await TestDataSeeder.SeedTestDivisionsAndTeamsAsync(_fixture.DbContext);

        var teams = await _fixture.DbContext.Teams.ToListAsync();
        var teamId = teams.First().Id;

        var command = new UpdateTeamCommand(
            Id: teamId,
            Name: "Unauthorized Team",
            LogoUrl: null,
            Founded: 2020,
            HomeGround: null,
            City: null,
            PrimaryColour: null,
            SecondaryColour: null
        );

        // Act - No token set
        var response = await _fixture.HttpClient.PutAsJsonAsync(
            $"/api/teams/{teamId}",
            command
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateTeam_NonExistentTeam_Returns404()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);

        var token = await GetSuperAdminTokenAsync();
        _fixture.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var nonExistentTeamId = Guid.NewGuid();

        var command = new UpdateTeamCommand(
            Id: nonExistentTeamId,
            Name: "Ghost Team",
            LogoUrl: null,
            Founded: 2020,
            HomeGround: null,
            City: null,
            PrimaryColour: null,
            SecondaryColour: null
        );

        // Act
        var response = await _fixture.HttpClient.PutAsJsonAsync(
            $"/api/teams/{nonExistentTeamId}",
            command
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTeam_TracksAuditLog()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);
        await TestDataSeeder.SeedTestDivisionsAndTeamsAsync(_fixture.DbContext);

        var token = await GetSuperAdminTokenAsync();
        _fixture.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var teams = await _fixture.DbContext.Teams.ToListAsync();
        var teamId = teams.First().Id;

        var command = new UpdateTeamCommand(
            Id: teamId,
            Name: "Audited Team",
            LogoUrl: null,
            Founded: 2020,
            HomeGround: "Audit Stadium",
            City: null,
            PrimaryColour: null,
            SecondaryColour: null
        );

        // Act
        var response = await _fixture.HttpClient.PutAsJsonAsync(
            $"/api/teams/{teamId}",
            command
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify audit log was created
        var auditLogs = _fixture.DbContext.AuditLogs
            .Where(al => al.EntityType == "Team" && al.EntityId == teamId)
            .ToList();

        auditLogs.Should().NotBeEmpty();
        var latestLog = auditLogs.OrderByDescending(al => al.ChangedAt).First();
        latestLog.Action.Should().Be("Updated");
        latestLog.NewValues.Should().Contain("Audited Team");
    }
}
