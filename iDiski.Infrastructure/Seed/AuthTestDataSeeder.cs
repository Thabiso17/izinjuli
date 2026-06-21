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
                Console.WriteLine("✅ Auth test users already seeded, skipping...");
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
        }
    }
}
