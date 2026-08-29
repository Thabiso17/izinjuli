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
/// Seeds test authentication users (SuperAdmin, TeamAdmin, DivisionAdmin, InactiveUser)
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

            // DivisionAdmin User
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

            db.Users.AddRange(superAdmin, teamAdmin, divisionAdmin, inactiveUser);
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

            var divisionAdminRole = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = divisionAdminId,
                Role = Role.DivisionAdmin,
                AssignedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            db.UserRoles.AddRange(superAdminRole, teamAdminRole, divisionAdminRole);
            await db.SaveChangesAsync();

            Console.WriteLine("✅ Roles assigned successfully");
            Console.WriteLine("\n🔐 Test Credentials:");
            Console.WriteLine("   SuperAdmin:    superadmin@test.com / Password123!");
            Console.WriteLine("   TeamAdmin:     teamadmin@test.com / Password123!");
            Console.WriteLine("   DivisionAdmin: divadmin@test.com / Password123!");
            Console.WriteLine("   InactiveUser:  inactive@test.com / Password123! (should fail)\n");

            await AssignSampleOwnershipAsync(db);
        }
    }

    /// <summary>
    /// Scopes divadmin@test.com/teamadmin@test.com to a sample Division/Team so their
    /// resource-ownership checks are actually exercisable in dev. Divisions/Teams are
    /// usually populated later via /api/seed, so this re-checks on every startup rather
    /// than only right after user creation, and no-ops once an assignment already exists.
    /// </summary>
    private static async Task AssignSampleOwnershipAsync(LeagueDbContext db)
    {
        var divisionAdmin = await db.Users.FirstOrDefaultAsync(u => u.Email == "divadmin@test.com");
        if (divisionAdmin != null && !await db.UserDivisions.AnyAsync(ud => ud.UserId == divisionAdmin.Id))
        {
            var division = await db.Divisions.OrderBy(d => d.CreatedAt).FirstOrDefaultAsync();
            if (division != null)
            {
                db.UserDivisions.Add(new UserDivision
                {
                    Id = Guid.NewGuid(),
                    UserId = divisionAdmin.Id,
                    DivisionId = division.Id,
                    AssignedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
                Console.WriteLine($"✅ Assigned divadmin@test.com to division '{division.Name}'");
            }
            else
            {
                Console.WriteLine("⚠️ No divisions exist yet — divadmin@test.com has no division assignment. Seed divisions (e.g. /api/seed) and restart.");
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
