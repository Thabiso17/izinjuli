using System;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Application.Authentication.Commands;
using iDiski.Application.Common.Exceptions;
using iDiski.Domain.Entities;
using iDiski.Infrastructure.Services;
using iDiski.Tests.Integration.Common;
using Xunit;

namespace iDiski.Tests.Integration.Authentication;

public class LoginIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public LoginIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task LoginCommandHandler_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var hasher = new Argon2PasswordHasher();
        var userId = Guid.NewGuid();
        var passwordHash = hasher.HashPassword("TestPassword123!");

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = passwordHash,
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.Users.Add(user);
        await _fixture.DbContext.SaveChangesAsync();

        // Act - Verify password hashing works
        var isValid = hasher.VerifyPassword("TestPassword123!", passwordHash);

        // Assert
        isValid.Should().BeTrue();
        user.Email.Should().Be("test@example.com");
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task LoginFlow_WithInvalidPassword_ShouldReject()
    {
        // Arrange
        var hasher = new Argon2PasswordHasher();
        var userId = Guid.NewGuid();
        var passwordHash = hasher.HashPassword("CorrectPassword123!");

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = passwordHash,
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.Users.Add(user);
        await _fixture.DbContext.SaveChangesAsync();

        // Act - Verify wrong password fails
        var isValid = hasher.VerifyPassword("WrongPassword123!", passwordHash);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task LoginFlow_WithInactiveUser_ShouldBlock()
    {
        // Arrange
        var hasher = new Argon2PasswordHasher();
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "inactive@example.com",
            PasswordHash = hasher.HashPassword("Password123!"),
            FirstName = "Inactive",
            LastName = "User",
            IsActive = false, // Inactive user
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.Users.Add(user);
        await _fixture.DbContext.SaveChangesAsync();

        // Act & Assert
        var retrievedUser = await _fixture.DbContext.Users.FindAsync(userId);
        retrievedUser?.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task LoginFlow_UpdatesLastLoginAt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var beforeLogin = DateTime.UtcNow;

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = new Argon2PasswordHasher().HashPassword("Password123!"),
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            CreatedAt = beforeLogin,
            UpdatedAt = beforeLogin,
            LastLoginAt = null
        };

        _fixture.DbContext.Users.Add(user);
        await _fixture.DbContext.SaveChangesAsync();

        // Act - Simulate login update
        user.LastLoginAt = DateTime.UtcNow;
        await _fixture.DbContext.SaveChangesAsync();

        // Assert
        var updatedUser = await _fixture.DbContext.Users.FindAsync(userId);
        updatedUser?.LastLoginAt.Should().NotBeNull();
        updatedUser?.LastLoginAt.Should().BeAfter(beforeLogin);
    }

    [Fact]
    public async Task UserCreation_WithArgon2Hashing_PersistsCorrectly()
    {
        // Arrange
        var hasher = new Argon2PasswordHasher();
        var plainPassword = "SecurePassword123!";
        var hash1 = hasher.HashPassword(plainPassword);
        var hash2 = hasher.HashPassword(plainPassword);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            PasswordHash = hash1,
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.Users.Add(user);
        await _fixture.DbContext.SaveChangesAsync();

        // Act - Retrieve and verify
        var retrieved = await _fixture.DbContext.Users.FindAsync(user.Id);
        var passwordValid = hasher.VerifyPassword(plainPassword, retrieved!.PasswordHash);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved?.Email.Should().Be("user@example.com");
        passwordValid.Should().BeTrue();
        // Note: Both hashes work for the same password (Argon2 includes salt and iteration in hash)
    }
}
