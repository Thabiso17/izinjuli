using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Application.Authentication.Commands;
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
    public async Task Login_WithValidCredentials_ReturnsJwtToken()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);

        var request = new LoginCommand(
            Email: "superadmin@test.com",
            Password: "Password123!"
        );

        // Act
        var response = await _fixture.HttpClient.PostAsJsonAsync(
            "/api/authentication/login",
            request
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsAsync<LoginResponse>();
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be("superadmin@test.com");

        // Verify JWT is valid
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.AccessToken);
        token.Should().NotBeNull();
        token.Claims.Should().ContainSingle(c => c.Type == "email" && c.Value == "superadmin@test.com");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns401Unauthorized()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);

        var request = new LoginCommand(
            Email: "superadmin@test.com",
            Password: "WrongPassword123!"
        );

        // Act
        var response = await _fixture.HttpClient.PostAsJsonAsync(
            "/api/authentication/login",
            request
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401Unauthorized()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);

        var request = new LoginCommand(
            Email: "nonexistent@test.com",
            Password: "Password123!"
        );

        // Act
        var response = await _fixture.HttpClient.PostAsJsonAsync(
            "/api/authentication/login",
            request
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithInactiveUser_Returns403Forbidden()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);

        var request = new LoginCommand(
            Email: "inactive@test.com",
            Password: "Password123!"
        );

        // Act
        var response = await _fixture.HttpClient.PostAsJsonAsync(
            "/api/authentication/login",
            request
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Login_UpdatesLastLoginAt()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);

        var user = await _fixture.DbContext.Users.FindAsync(
            new Guid("00000000-0000-0000-0000-000000000001")
        );
        var originalLastLogin = user?.LastLoginAt;

        var request = new LoginCommand(
            Email: "superadmin@test.com",
            Password: "Password123!"
        );

        // Act
        await _fixture.HttpClient.PostAsJsonAsync(
            "/api/authentication/login",
            request
        );

        // Assert
        var updatedUser = await _fixture.DbContext.Users.FindAsync(
            new Guid("00000000-0000-0000-0000-000000000001")
        );
        updatedUser?.LastLoginAt.Should().BeAfter(originalLastLogin ?? DateTime.MinValue);
    }

    [Fact]
    public async Task Login_IncludesUserRolesInToken()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);

        var request = new LoginCommand(
            Email: "superadmin@test.com",
            Password: "Password123!"
        );

        // Act
        var response = await _fixture.HttpClient.PostAsJsonAsync(
            "/api/authentication/login",
            request
        );

        // Assert
        var result = await response.Content.ReadAsAsync<LoginResponse>();
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.AccessToken);

        // Verify role claim exists
        var roleClaim = token.Claims.FirstOrDefault(c => c.Type == "role");
        roleClaim.Should().NotBeNull();
        roleClaim?.Value.Should().Contain("SuperAdmin");
    }

    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = new();
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}
