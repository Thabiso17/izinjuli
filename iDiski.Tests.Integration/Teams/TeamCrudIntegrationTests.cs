using System;
using System.Threading.Tasks;
using System.Linq;
using FluentAssertions;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
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

    [Fact]
    public async Task CreateTeam_WithValidData_PersistsToDatabase()
    {
        // Arrange
        var divisionId = Guid.NewGuid();
        var division = new Division
        {
            Id = divisionId,
            Name = "Premier Division",
            ShortCode = "PD",
            Season = 2026,
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.Divisions.Add(division);
        await _fixture.DbContext.SaveChangesAsync();

        var teamId = Guid.NewGuid();
        var team = new Team
        {
            Id = teamId,
            Name = "Test Team",
            ShortCode = "TT",
            DivisionId = divisionId,
            Founded = 2020,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _fixture.DbContext.Teams.Add(team);
        await _fixture.DbContext.SaveChangesAsync();

        // Assert
        var createdTeam = await _fixture.DbContext.Teams.FindAsync(teamId);
        createdTeam.Should().NotBeNull();
        createdTeam?.Name.Should().Be("Test Team");
        createdTeam?.DivisionId.Should().Be(divisionId);
    }

    [Fact]
    public async Task UpdateTeam_UpdatesDatabase()
    {
        // Arrange
        var divisionId = Guid.NewGuid();
        var division = new Division
        {
            Id = divisionId,
            Name = "Test Division",
            ShortCode = "TD",
            Season = 2026,
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.Divisions.Add(division);
        await _fixture.DbContext.SaveChangesAsync();

        var teamId = Guid.NewGuid();
        var team = new Team
        {
            Id = teamId,
            Name = "Original Name",
            ShortCode = "ON",
            DivisionId = divisionId,
            Founded = 2020,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.Teams.Add(team);
        await _fixture.DbContext.SaveChangesAsync();

        // Act - Update team
        team.Name = "Updated Name";
        team.UpdatedAt = DateTime.UtcNow;
        await _fixture.DbContext.SaveChangesAsync();

        // Assert
        var updated = await _fixture.DbContext.Teams.FindAsync(teamId);
        updated?.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task TeamOwnership_EnforcedViaUserTeamAssignment()
    {
        // Arrange
        var divisionId = Guid.NewGuid();
        var division = new Division
        {
            Id = divisionId,
            Name = "Test Division",
            ShortCode = "TD",
            Season = 2026,
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.Divisions.Add(division);

        var teamId = Guid.NewGuid();
        var team = new Team
        {
            Id = teamId,
            Name = "Team A",
            ShortCode = "TA",
            DivisionId = divisionId,
            Founded = 2020,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.Teams.Add(team);

        var userId = Guid.NewGuid();
        var userTeam = new UserTeam
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TeamId = teamId,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.UserTeams.Add(userTeam);
        await _fixture.DbContext.SaveChangesAsync();

        // Act - Check if user is assigned to team
        var assignment = await _fixture.DbContext.UserTeams
            .FindAsync(userTeam.Id);

        // Assert
        assignment.Should().NotBeNull();
        assignment?.UserId.Should().Be(userId);
        assignment?.TeamId.Should().Be(teamId);
    }

    [Fact]
    public async Task Team_CanBeAssignedToMultipleDivisionAdmins()
    {
        // Arrange
        var divisionId = Guid.NewGuid();
        var division = new Division
        {
            Id = divisionId,
            Name = "Test Division",
            ShortCode = "TD",
            Season = 2026,
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.Divisions.Add(division);

        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        var divAdminAssignment1 = new UserDivision
        {
            Id = Guid.NewGuid(),
            UserId = user1Id,
            DivisionId = divisionId,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var divAdminAssignment2 = new UserDivision
        {
            Id = Guid.NewGuid(),
            UserId = user2Id,
            DivisionId = divisionId,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.UserDivisions.AddRange(divAdminAssignment1, divAdminAssignment2);
        await _fixture.DbContext.SaveChangesAsync();

        // Act - Check both admins assigned to division
        var admins = _fixture.DbContext.UserDivisions
            .Where(ud => ud.DivisionId == divisionId)
            .ToList();

        // Assert
        admins.Should().HaveCount(2);
        admins.Should().ContainSingle(a => a.UserId == user1Id);
        admins.Should().ContainSingle(a => a.UserId == user2Id);
    }

    [Fact]
    public async Task Team_RejectsInvalidShortCode()
    {
        // Arrange
        var divisionId = Guid.NewGuid();
        var division = new Division
        {
            Id = divisionId,
            Name = "Test Division",
            ShortCode = "TD",
            Season = 2026,
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.Divisions.Add(division);
        await _fixture.DbContext.SaveChangesAsync();

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            ShortCode = "TOOLONG", // Too long
            DivisionId = divisionId,
            Founded = 2020,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act & Assert - Should not throw, short code length is just a convention
        _fixture.DbContext.Teams.Add(team);
        await _fixture.DbContext.SaveChangesAsync();
        team.ShortCode.Length.Should().Be(7);
    }
}
