using System;
using System.Linq;
using System.Threading.Tasks;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
using iDiski.Infrastructure.Persistence;
using iDiski.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace iDiski.Infrastructure.Seed;

/// <summary>
/// Seeds test authentication users (SuperAdmin, TeamAdmin, CompetitionAdmin, InactiveUser)
/// for local development and testing.
/// </summary>
public static class AuthTestDataSeeder
{
    public static async Task SeedAuthTestUsers(IServiceProvider services)
    {
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LeagueDbContext>();
            var hasher = new Argon2PasswordHasher();

            // Check if users already exist
            if (await db.Users.AnyAsync(u => u.Email == "superadmin@test.com"))
            {
                Console.WriteLine("✅ Auth test users already seeded, skipping user creation...");
                await AssignSampleOwnershipAsync(db);
                return;
            }

            Console.WriteLine("🌱 Seeding auth test users...");

            // SuperAdmin User
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

            // TeamAdmin User
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

            // CompetitionAdmin User
            var competitionAdminId = Guid.NewGuid();
            var competitionAdmin = new User
            {
                Id = competitionAdminId,
                Email = "divadmin@test.com",
                PasswordHash = hasher.HashPassword("Password123!"),
                FirstName = "Division",
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

            db.Users.AddRange(superAdmin, teamAdmin, competitionAdmin, inactiveUser);
            await db.SaveChangesAsync();

            Console.WriteLine("✅ Users created successfully");

            // Assign roles
            var superAdminRole = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = superAdminId,
                Role = Role.SuperAdmin,
                AssignedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            var teamAdminRole = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = teamAdminId,
                Role = Role.TeamAdmin,
                AssignedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            var competitionAdminRole = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = competitionAdminId,
                Role = Role.CompetitionAdmin,
                AssignedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            db.UserRoles.AddRange(superAdminRole, teamAdminRole, competitionAdminRole);
            await db.SaveChangesAsync();

            Console.WriteLine("✅ Roles assigned successfully");
            Console.WriteLine("\n🔐 Test Credentials:");
            Console.WriteLine("   SuperAdmin:    superadmin@test.com / Password123!");
            Console.WriteLine("   TeamAdmin:     teamadmin@test.com / Password123!");
            Console.WriteLine("   CompetitionAdmin: divadmin@test.com / Password123!");
            Console.WriteLine("   InactiveUser:  inactive@test.com / Password123! (should fail)\n");

            await AssignSampleOwnershipAsync(db);
        }
    }

    /// <summary>
    /// Scopes divadmin@test.com/teamadmin@test.com to a sample Competition/Team so their
    /// resource-ownership checks are actually exercisable in dev. Competitions/Teams are
    /// usually populated later via /api/seed, so this re-checks on every startup rather
    /// than only right after user creation, and no-ops once an assignment already exists.
    /// </summary>
    private static async Task AssignSampleOwnershipAsync(LeagueDbContext db)
    {
        var competitionAdmin = await db.Users.FirstOrDefaultAsync(u => u.Email == "divadmin@test.com");
        if (competitionAdmin != null
            && !await db.UserCompetitions.AnyAsync(uc => uc.UserId == competitionAdmin.Id))
        {
            var competition = await db.Competitions.OrderBy(c => c.CreatedAt).FirstOrDefaultAsync();
            if (competition != null)
            {
                db.UserCompetitions.Add(new UserCompetition
                {
                    Id = Guid.NewGuid(),
                    UserId = competitionAdmin.Id,
                    CompetitionId = competition.Id,
                    AssignedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
                Console.WriteLine($"✅ Assigned divadmin@test.com to competition '{competition.Name}'");
            }
            else
            {
                Console.WriteLine("⚠️ No competitions exist yet — divadmin@test.com runs nothing. Seed (e.g. /api/seed) and restart.");
            }
        }

        var teamAdmin = await db.Users.FirstOrDefaultAsync(u => u.Email == "teamadmin@test.com");
        if (teamAdmin != null && !await db.UserTeams.AnyAsync(ut => ut.UserId == teamAdmin.Id))
        {
            var team = await db.Teams.OrderBy(t => t.CreatedAt).FirstOrDefaultAsync();
            if (team != null)
            {
                db.UserTeams.Add(new UserTeam
                {
                    Id = Guid.NewGuid(),
                    UserId = teamAdmin.Id,
                    TeamId = team.Id,
                    AssignedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
                Console.WriteLine($"✅ Assigned teamadmin@test.com to team '{team.Name}'");
            }
            else
            {
                Console.WriteLine("⚠️ No teams exist yet — teamadmin@test.com has no team assignment. Seed teams (e.g. /api/seed) and restart.");
            }
        }

        await db.SaveChangesAsync();
    }
}
