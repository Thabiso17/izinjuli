using System;
using System.Threading.Tasks;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
using iDiski.Infrastructure.Persistence;
using iDiski.Infrastructure.Services;

namespace iDiski.Tests.Integration.Common;

public static class TestDataSeeder
{
    public static async Task SeedTestUsersAsync(LeagueDbContext dbContext)
    {
        var hasher = new Argon2PasswordHasher();

        // Super Admin User
        var superAdminId = Guid.NewGuid();
        var superAdmin = new User
        {
            Id = superAdminId,
            Email = "superadmin@test.com",
            PasswordHash = hasher.HashPassword("Password123!"),
            FirstName = "Super",
            LastName = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Division Admin User
        var divisionAdminId = Guid.NewGuid();
        var divisionAdmin = new User
        {
            Id = divisionAdminId,
            Email = "divadmin@test.com",
            PasswordHash = hasher.HashPassword("Password123!"),
            FirstName = "Division",
            LastName = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Team Admin User
        var teamAdminId = Guid.NewGuid();
        var teamAdmin = new User
        {
            Id = teamAdminId,
            Email = "teamadmin@test.com",
            PasswordHash = hasher.HashPassword("Password123!"),
            FirstName = "Team",
            LastName = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Inactive User
        var inactiveUserId = Guid.NewGuid();
        var inactiveUser = new User
        {
            Id = inactiveUserId,
            Email = "inactive@test.com",
            PasswordHash = hasher.HashPassword("Password123!"),
            FirstName = "Inactive",
            LastName = "User",
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Users.AddRange(superAdmin, divisionAdmin, teamAdmin, inactiveUser);

        // Assign roles
        dbContext.UserRoles.AddRange(
            new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = superAdminId,
                Role = Role.SuperAdmin,
                AssignedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            },
            new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = divisionAdminId,
                Role = Role.DivisionAdmin,
                AssignedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            },
            new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = teamAdminId,
                Role = Role.TeamAdmin,
                AssignedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }
        );

        await dbContext.SaveChangesAsync();
    }

    public static async Task SeedTestDivisionsAndTeamsAsync(LeagueDbContext dbContext)
    {
        var division = new Division
        {
            Id = Guid.NewGuid(),
            ShortCode = "DIV1",
            Name = "Premier Division",
            Season = 2026,
            Gender = Gender.Male,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var team1 = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team A",
            ShortCode = "TEA",
            DivisionId = division.Id,
            Founded = 2020,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var team2 = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Team B",
            ShortCode = "TEB",
            DivisionId = division.Id,
            Founded = 2021,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Divisions.Add(division);
        dbContext.Teams.AddRange(team1, team2);

        // Assign teams to team admin
        var teamAdminId = new Guid("00000000-0000-0000-0000-000000000003");
        dbContext.UserTeams.Add(new UserTeam
        {
            Id = Guid.NewGuid(),
            UserId = teamAdminId,
            TeamId = team1.Id,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        // Assign division to division admin
        var divisionAdminId = new Guid("00000000-0000-0000-0000-000000000002");
        dbContext.UserDivisions.Add(new UserDivision
        {
            Id = Guid.NewGuid(),
            UserId = divisionAdminId,
            DivisionId = division.Id,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }
}
