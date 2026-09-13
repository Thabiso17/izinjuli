using System;
using System.Threading.Tasks;
using System.Linq;
using FluentAssertions;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
using iDiski.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace iDiski.Tests.Integration.Teams;

/// <summary>
/// Teams against a real database. Every test in the class shares one, so each seeds its own
/// division and team with codes that cannot clash with a neighbour's — Divisions carry a
/// unique index on (Season, ShortCode) and Teams one on ShortCode, and anything assigning a
/// user has to create that user first, because the join tables have real foreign keys.
/// </summary>
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
        var division = await SeedDivisionAsync("Premier Division");

        var teamId = Guid.NewGuid();
        var team = new Team
        {
            Id = teamId,
            Name = "Test Team",
            ShortCode = Code("TT"),
            DivisionId = division.Id,
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
        createdTeam?.DivisionId.Should().Be(division.Id);
    }

    [Fact]
    public async Task UpdateTeam_UpdatesDatabase()
    {
        // Arrange
        var division = await SeedDivisionAsync("Test Division");

        var teamId = Guid.NewGuid();
        var team = new Team
        {
            Id = teamId,
            Name = "Original Name",
            ShortCode = Code("ON"),
            DivisionId = division.Id,
            Founded = 2020,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.Teams.Add(team);
        await _fixture.DbContext.SaveChangesAsync();

        // Act - Update team
        team.Name = "Updated Name";
        await _fixture.DbContext.SaveChangesAsync();

        // Assert
        var updated = await _fixture.DbContext.Teams.FindAsync(teamId);
        updated?.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task TeamOwnership_EnforcedViaUserTeamAssignment()
    {
        // Arrange
        var division = await SeedDivisionAsync("Test Division");

        var teamId = Guid.NewGuid();
        _fixture.DbContext.Teams.Add(new Team
        {
            Id = teamId,
            Name = "Team A",
            ShortCode = Code("TA"),
            DivisionId = division.Id,
            Founded = 2020,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // The assignment points at a real user: UserTeams has a foreign key to Users, so an
        // assignment for a user that was never created is rejected by the database.
        var user = await SeedUserAsync("team-admin");

        var userTeam = new UserTeam
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TeamId = teamId,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.UserTeams.Add(userTeam);
        await _fixture.DbContext.SaveChangesAsync();

        // Act - Check if user is assigned to team
        var assignment = await _fixture.DbContext.UserTeams.FindAsync(userTeam.Id);

        // Assert
        assignment.Should().NotBeNull();
        assignment?.UserId.Should().Be(user.Id);
        assignment?.TeamId.Should().Be(teamId);
    }

    [Fact]
    public async Task AssignmentToAUserThatDoesNotExist_IsRejected()
    {
        var division = await SeedDivisionAsync("Orphan Division");

        var orphan = new UserDivision
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(), // never created
            DivisionId = division.Id,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.UserDivisions.Add(orphan);

        var save = async () => await _fixture.DbContext.SaveChangesAsync();
        await save.Should().ThrowAsync<DbUpdateException>(
            "an assignment granting access to nobody would leave a division unreachable");

        // A failed insert stays tracked as Added and would be retried on the next save,
        // breaking whichever test saves after this one.
        _fixture.DbContext.Entry(orphan).State = EntityState.Detached;
    }

    [Fact]
    public async Task Division_CanBeAssignedToMultipleDivisionAdmins()
    {
        // Arrange
        var division = await SeedDivisionAsync("Test Division");

        var user1 = await SeedUserAsync("division-admin-1");
        var user2 = await SeedUserAsync("division-admin-2");

        _fixture.DbContext.UserDivisions.AddRange(
            new UserDivision
            {
                Id = Guid.NewGuid(),
                UserId = user1.Id,
                DivisionId = division.Id,
                AssignedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            },
            new UserDivision
            {
                Id = Guid.NewGuid(),
                UserId = user2.Id,
                DivisionId = division.Id,
                AssignedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });

        await _fixture.DbContext.SaveChangesAsync();

        // Act - Check both admins assigned to division
        var admins = await _fixture.DbContext.UserDivisions
            .Where(ud => ud.DivisionId == division.Id)
            .ToListAsync();

        // Assert
        admins.Should().HaveCount(2);
        admins.Should().ContainSingle(a => a.UserId == user1.Id);
        admins.Should().ContainSingle(a => a.UserId == user2.Id);
    }

    [Fact]
    public async Task Team_AcceptsAnyShortCodeWithinTheColumnLength()
    {
        // Arrange
        var division = await SeedDivisionAsync("Test Division");

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            ShortCode = "TOOLONG",
            DivisionId = division.Id,
            Founded = 2020,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act & Assert - seven characters is longer than a club would normally use, but the
        // column allows ten, so the database has no reason to refuse it.
        _fixture.DbContext.Teams.Add(team);
        await _fixture.DbContext.SaveChangesAsync();
        team.ShortCode.Length.Should().Be(7);
    }

    [Fact]
    public async Task Team_RejectsAShortCodeLongerThanTheColumn()
    {
        var division = await SeedDivisionAsync("Long Code Division");

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team",
            ShortCode = "ELEVENCHARS", // eleven, against a ten-character column
            DivisionId = division.Id,
            Founded = 2020,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.Teams.Add(team);

        var save = async () => await _fixture.DbContext.SaveChangesAsync();
        await save.Should().ThrowAsync<DbUpdateException>();

        _fixture.DbContext.Entry(team).State = EntityState.Detached;
    }

    [Fact]
    public async Task Team_RejectsADuplicateShortCode()
    {
        var division = await SeedDivisionAsync("Duplicate Code Division");
        var shortCode = Code("DC");

        _fixture.DbContext.Teams.Add(new Team
        {
            Id = Guid.NewGuid(),
            Name = "First Team",
            ShortCode = shortCode,
            DivisionId = division.Id,
            Founded = 2020,
            CreatedAt = DateTime.UtcNow
        });
        await _fixture.DbContext.SaveChangesAsync();

        var duplicate = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Second Team",
            ShortCode = shortCode,
            DivisionId = division.Id,
            Founded = 2021,
            CreatedAt = DateTime.UtcNow
        };
        _fixture.DbContext.Teams.Add(duplicate);

        var save = async () => await _fixture.DbContext.SaveChangesAsync();
        await save.Should().ThrowAsync<DbUpdateException>(
            "two clubs sharing a short code would collide everywhere it is used as a label");

        _fixture.DbContext.Entry(duplicate).State = EntityState.Detached;
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Divisions carry a unique index on (Season, ShortCode), and every test in this class
    /// writes to the same database, so each one needs a code of its own.
    /// </summary>
    private async Task<Division> SeedDivisionAsync(string name)
    {
        var division = new Division
        {
            Id = Guid.NewGuid(),
            Name = name,
            ShortCode = Code("D"),
            Season = 2026,
            Gender = Gender.Male,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.Divisions.Add(division);
        await _fixture.DbContext.SaveChangesAsync();
        return division;
    }

    private async Task<User> SeedUserAsync(string label)
    {
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            Email = $"{label}-{id:N}@test.com",
            PasswordHash = "not-used-here",
            FirstName = "Test",
            LastName = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.Users.Add(user);
        await _fixture.DbContext.SaveChangesAsync();
        return user;
    }

    /// <summary>A short code unique to this test, inside the column's length limit.</summary>
    private static string Code(string prefix) => TestIds.Code(prefix);
}
