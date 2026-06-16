using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Tests.Integration.Common;
using Xunit;

namespace iDiski.Tests.Integration.Users;

public class UserManagementIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public UserManagementIntegrationTests(IntegrationTestFixture fixture)
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
    public async Task CreateUser_AsSuperAdmin_Succeeds()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);

        var token = await GetSuperAdminTokenAsync();
        _fixture.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var createUserRequest = new
        {
            email = "newuser@test.com",
            password = "NewPassword123!",
            firstName = "New",
            lastName = "User",
            role = 1 // TeamAdmin
        };

        // Act
        var response = await _fixture.HttpClient.PostAsJsonAsync(
            "/api/users",
            createUserRequest
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdUser = await _fixture.DbContext.Users
            .FirstOrDefaultAsync(u => u.Email == "newuser@test.com");
        createdUser.Should().NotBeNull();
        createdUser?.FirstName.Should().Be("New");
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_Returns422()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);

        var token = await GetSuperAdminTokenAsync();
        _fixture.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var createUserRequest = new
        {
            email = "superadmin@test.com", // Already exists
            password = "NewPassword123!",
            firstName = "Duplicate",
            lastName = "User",
            role = 1
        };

        // Act
        var response = await _fixture.HttpClient.PostAsJsonAsync(
            "/api/users",
            createUserRequest
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateUser_WithWeakPassword_Returns422()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);

        var token = await GetSuperAdminTokenAsync();
        _fixture.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var createUserRequest = new
        {
            email = "weak@test.com",
            password = "weak", // Too short, no special chars
            firstName = "Weak",
            lastName = "Pass",
            role = 1
        };

        // Act
        var response = await _fixture.HttpClient.PostAsJsonAsync(
            "/api/users",
            createUserRequest
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateUser_AsNonSuperAdmin_Returns403()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);

        var token = await GetTeamAdminTokenAsync();
        _fixture.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var createUserRequest = new
        {
            email = "restricted@test.com",
            password = "Password123!",
            firstName = "Restricted",
            lastName = "User",
            role = 1
        };

        // Act
        var response = await _fixture.HttpClient.PostAsJsonAsync(
            "/api/users",
            createUserRequest
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_WithoutAuthentication_Returns401()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);

        var createUserRequest = new
        {
            email = "unauthenticated@test.com",
            password = "Password123!",
            firstName = "No",
            lastName = "Auth",
            role = 1
        };

        // Act - No token set
        var response = await _fixture.HttpClient.PostAsJsonAsync(
            "/api/users",
            createUserRequest
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsAuthenticatedUser()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);

        var token = await GetSuperAdminTokenAsync();
        _fixture.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _fixture.HttpClient.GetAsync(
            "/api/authentication/me"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await response.Content.ReadAsAsync<dynamic>();
        ((string)user.email).Should().Be("superadmin@test.com");
        ((string)user.firstName).Should().Be("Super");
    }

    [Fact]
    public async Task GetCurrentUser_WithoutAuthentication_Returns401()
    {
        // Arrange - No token set

        // Act
        var response = await _fixture.HttpClient.GetAsync(
            "/api/authentication/me"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUser_BySuperAdmin_ReturnsUserWithRoles()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);

        var token = await GetSuperAdminTokenAsync();
        _fixture.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var teamAdminId = new Guid("00000000-0000-0000-0000-000000000003");

        // Act
        var response = await _fixture.HttpClient.GetAsync(
            $"/api/users/{teamAdminId}"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await response.Content.ReadAsAsync<dynamic>();
        ((string)user.email).Should().Be("teamadmin@test.com");
        ((object[])user.roleIds).Length.Should().Be(1); // TeamAdmin role
    }

    [Fact]
    public async Task ListUsers_BySuperAdmin_ReturnsAllUsers()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);

        var token = await GetSuperAdminTokenAsync();
        _fixture.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _fixture.HttpClient.GetAsync(
            "/api/users"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await response.Content.ReadAsAsync<dynamic>();
        ((object[])users).Length.Should().BeGreaterThanOrEqualTo(4); // At least our 4 test users
    }

    [Fact]
    public async Task ListUsers_AsNonSuperAdmin_Returns403()
    {
        // Arrange
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);

        var token = await GetTeamAdminTokenAsync();
        _fixture.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _fixture.HttpClient.GetAsync(
            "/api/users"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
