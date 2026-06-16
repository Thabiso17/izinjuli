# Integration Testing Guide

## Overview

Integration tests validate complete end-to-end flows in iDiski: HTTP requests → authentication → authorization → database operations → HTTP responses. They catch real-world issues that unit tests miss.

**Key Difference from Unit Tests:**
- **Unit Tests**: Mock all dependencies, test isolated logic
- **Integration Tests**: Use real database, real HTTP, real middleware

---

## Running Integration Tests

### All Tests
```bash
dotnet test iDiski.Tests.Integration --configuration Release
```

### Specific Suite
```bash
dotnet test iDiski.Tests.Integration --filter "FullyQualifiedName~LoginIntegrationTests"
dotnet test iDiski.Tests.Integration --filter "FullyQualifiedName~TeamCrudIntegrationTests"
```

### With Verbose Output
```bash
dotnet test iDiski.Tests.Integration --verbosity detailed
```

### Using Test Runner Script
```bash
# macOS/Linux
chmod +x run-integration-tests.sh
./run-integration-tests.sh

# Windows (PowerShell script coming soon)
```

---

## Test Suites

### 1. Authentication Flow (`LoginIntegrationTests`)

Tests the complete login flow from HTTP request to JWT token generation.

#### Positive Tests
- ✅ **Login_WithValidCredentials_ReturnsJwtToken**
  - User logs in with correct email/password
  - Returns JWT token with user info and role claims
  - Token is valid and can be decoded

- ✅ **Login_UpdatesLastLoginAt**
  - Successful login updates `LastLoginAt` timestamp
  - Verifies audit trail functionality

- ✅ **Login_IncludesUserRolesInToken**
  - JWT token includes user roles in claims
  - Frontend can decode and check permissions without server round-trip

#### Negative Tests
- ❌ **Login_WithInvalidPassword_Returns401Unauthorized**
  - Wrong password → 401 Unauthorized
  - No token issued
  - No user enumeration vulnerability

- ❌ **Login_WithNonExistentEmail_Returns401Unauthorized**
  - Non-existent email → 401 Unauthorized
  - Same response as invalid password (security)

- ❌ **Login_WithInactiveUser_Returns403Forbidden**
  - User with `IsActive = false` cannot log in
  - Returns 403 Forbidden (different from 401 - indicates auth succeeded but user disabled)

**Why These Tests Matter:**
- Validates JWT generation actually works (not just unit test mock)
- Ensures password hashing validation works end-to-end
- Catches serialization bugs in JWT claims
- Verifies authentication middleware is wired correctly

---

### 2. Team CRUD Operations (`TeamCrudIntegrationTests`)

Tests team creation, reading, updating with authorization checks.

#### Positive Tests
- ✅ **GetTeams_ReturnsAllTeams_WithoutAuthentication**
  - Public read endpoint works without token
  - All teams returned correctly

- ✅ **UpdateTeam_AsSuperAdmin_Succeeds**
  - SuperAdmin can update any team
  - Changes persisted to database
  - No authorization check blocks it

- ✅ **UpdateTeam_AsTeamAdmin_OnAssignedTeam_Succeeds**
  - TeamAdmin can update their assigned team
  - `UpdatedByUserId` audit field is set
  - Database transaction completes

- ✅ **UpdateTeam_TracksAuditLog**
  - Team update creates audit log entry
  - `EntityType`, `Action`, `OldValues`, `NewValues` recorded
  - Queryable via audit API

#### Negative Tests
- ❌ **UpdateTeam_AsTeamAdmin_OnUnassignedTeam_Returns403**
  - TeamAdmin attempting to update unassigned team → 403
  - Authorization handler correctly rejects
  - Prevents privilege escalation

- ❌ **UpdateTeam_WithoutAuthentication_Returns401**
  - Update without JWT → 401 Unauthorized
  - Protected endpoint correctly guarded

- ❌ **UpdateTeam_NonExistentTeam_Returns404**
  - Update non-existent team → 404 Not Found
  - `NotFoundException` thrown correctly

**Why These Tests Matter:**
- Validates authorization hierarchy works in real HTTP flow
- Catches database persistence bugs
- Ensures audit trail is recorded for compliance
- Tests role-based access control (SuperAdmin vs TeamAdmin)
- Verifies ownership checks prevent unauthorized modifications

---

### 3. User Management (`UserManagementIntegrationTests`)

Tests user CRUD operations with SuperAdmin-only enforcement.

#### Positive Tests
- ✅ **CreateUser_AsSuperAdmin_Succeeds**
  - SuperAdmin creates user with email, password, name
  - User persisted with Argon2-hashed password
  - Returns 201 Created with new user ID

- ✅ **GetCurrentUser_ReturnsAuthenticatedUser**
  - Authenticated user calls `/me`
  - Receives profile from JWT claims (no database query needed)
  - Email, firstName, roles returned correctly

- ✅ **GetUser_BySuperAdmin_ReturnsUserWithRoles**
  - SuperAdmin retrieves any user by ID
  - Response includes assigned roles, teams, divisions

- ✅ **ListUsers_BySuperAdmin_ReturnsAllUsers**
  - SuperAdmin lists all users
  - Results paginated if needed
  - Includes role information

#### Negative Tests
- ❌ **CreateUser_WithDuplicateEmail_Returns422**
  - Duplicate email → 422 Unprocessable Entity
  - Validator catches before database
  - User not created, proper error response

- ❌ **CreateUser_WithWeakPassword_Returns422**
  - Password < 8 chars or missing uppercase/number/special → 422
  - FluentValidation enforced

- ❌ **CreateUser_AsNonSuperAdmin_Returns403**
  - TeamAdmin/DivisionAdmin attempting user creation → 403
  - Only SuperAdmin can create users

- ❌ **CreateUser_WithoutAuthentication_Returns401**
  - User creation without JWT → 401 Unauthorized

- ❌ **ListUsers_AsNonSuperAdmin_Returns403**
  - TeamAdmin attempting to list users → 403
  - Prevents user enumeration by non-admins

**Why These Tests Matter:**
- Validates SuperAdmin-only enforcement works
- Catches database constraint violations (email uniqueness)
- Ensures password validation happens before storage
- Prevents privilege escalation vulnerabilities
- Validates complete user creation workflow

---

## Test Data Seeding

The `TestDataSeeder` class creates consistent test data:

```csharp
// Seeds in every test via IClassFixture
await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);

// Creates:
// - SuperAdmin: superadmin@test.com / Password123!
// - DivisionAdmin: divadmin@test.com / Password123!
// - TeamAdmin: teamadmin@test.com / Password123!
// - InactiveUser: inactive@test.com / Password123! (IsActive=false)
//
// Also seeds divisions and teams with proper assignments
```

---

## Integration Test Fixture

The `IntegrationTestFixture` provides:

```csharp
public class IntegrationTestFixture : WebApplicationFactory<Program>
{
    // Creates test HTTP client
    public HttpClient HttpClient { get; }

    // Provides direct database access for assertions
    public LeagueDbContext DbContext { get; }

    // Lifecycle: Creates new in-memory database for each test
    // Automatically cleaned up after test completes
}
```

Usage:
```csharp
public class MyIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public MyIntegrationTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task MyTest()
    {
        // Use _fixture.HttpClient for HTTP requests
        // Use _fixture.DbContext for database assertions
    }
}
```

---

## Common Patterns

### Getting an Authentication Token
```csharp
private async Task<string> GetSuperAdminTokenAsync()
{
    var response = await _fixture.HttpClient.PostAsJsonAsync(
        "/api/authentication/login",
        new { email = "superadmin@test.com", password = "Password123!" }
    );

    var result = await response.Content.ReadAsAsync<dynamic>();
    return result.accessToken;
}

// In test:
var token = await GetSuperAdminTokenAsync();
_fixture.HttpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);
```

### Making Authenticated Requests
```csharp
var response = await _fixture.HttpClient.PutAsJsonAsync(
    "/api/teams/123",
    new { name = "Updated", ... }
);

response.StatusCode.Should().Be(HttpStatusCode.OK);
```

### Asserting Database State
```csharp
// After HTTP request, verify database was updated
var updatedTeam = await _fixture.DbContext.Teams.FindAsync(teamId);
updatedTeam?.Name.Should().Be("Updated");

// Verify audit log was created
var auditLogs = _fixture.DbContext.AuditLogs
    .Where(al => al.EntityType == "Team" && al.EntityId == teamId)
    .ToList();
auditLogs.Should().NotBeEmpty();
```

### Testing Authorization
```csharp
// Should fail without token
var response1 = await _fixture.HttpClient.GetAsync("/api/users");
response1.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

// Should fail with wrong role
var token = await GetTeamAdminTokenAsync();
_fixture.HttpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);
var response2 = await _fixture.HttpClient.GetAsync("/api/users");
response2.StatusCode.Should().Be(HttpStatusCode.Forbidden);

// Should succeed with right role
var token2 = await GetSuperAdminTokenAsync();
_fixture.HttpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token2);
var response3 = await _fixture.HttpClient.GetAsync("/api/users");
response3.StatusCode.Should().Be(HttpStatusCode.OK);
```

---

## Creating New Integration Tests

### Template
```csharp
using Xunit;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using iDiski.Tests.Integration.Common;

namespace iDiski.Tests.Integration.YourFeature;

public class YourFeatureIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public YourFeatureIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FeatureName_WithCondition_ExpectedOutcome()
    {
        // Arrange: Set up test data and state
        await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);
        
        var token = await GetSuperAdminTokenAsync();
        _fixture.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act: Make HTTP request
        var response = await _fixture.HttpClient.PostAsJsonAsync(
            "/api/endpoint",
            new { /* request data */ }
        );

        // Assert: Verify HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert: Verify database state
        var entity = await _fixture.DbContext.Entities
            .FirstOrDefaultAsync(e => e.Id == expectedId);
        entity.Should().NotBeNull();
        entity?.Property.Should().Be(expectedValue);
    }
}
```

### Naming Convention
- **Positive tests**: `FeatureName_WithValidInput_ReturnsExpected`
  - Example: `CreateUser_AsSuperAdmin_Succeeds`
  
- **Negative tests**: `FeatureName_WithInvalidCondition_Returns{StatusCode}`
  - Example: `CreateUser_WithDuplicateEmail_Returns422`

### What to Test
- ✅ Happy path (positive)
- ❌ Invalid input (negative)
- ❌ Unauthorized access (negative)
- ❌ Missing required fields (negative)
- ✅ Database persistence
- ✅ Audit trail creation
- ❌ Error messages are meaningful

---

## Continuous Integration

### GitHub Actions
```yaml
# .github/workflows/tests.yml
- name: Run integration tests
  run: dotnet test iDiski.Tests.Integration --configuration Release
```

### Before Committing
```bash
# Run all tests
dotnet test

# Or just integration tests
dotnet test iDiski.Tests.Integration

# Or specific suite
dotnet test iDiski.Tests.Integration --filter "FullyQualifiedName~TeamCrudIntegrationTests"
```

---

## Troubleshooting

### Tests Fail Locally But Pass on CI
- **Cause**: Database state not cleaned between runs
- **Fix**: `IntegrationTestFixture` creates fresh database for each test

### Timeout on Database Operations
- **Cause**: Test database locked or transaction hanging
- **Fix**: Check for missing `await` on async operations

### JWT Token Not Being Sent
- **Cause**: Forgot to set `DefaultRequestHeaders.Authorization`
- **Fix**: 
  ```csharp
  _fixture.HttpClient.DefaultRequestHeaders.Authorization =
      new AuthenticationHeaderValue("Bearer", token);
  ```

### Test Passes Locally But Fails on Different Machine
- **Cause**: Database path or timezone issues
- **Fix**: In-memory database should be portable. Check DateTime.UtcNow usage.

---

## Best Practices

1. **One Test = One Scenario**
   - Each `[Fact]` tests exactly one behavior
   - Multiple assertions within a test are OK if they test the same scenario

2. **Clear Test Names**
   - Name describes: what being tested + condition + expected outcome
   - Good: `UpdateTeam_AsTeamAdmin_OnUnassignedTeam_Returns403`
   - Bad: `UpdateTeamTest`

3. **Arrange-Act-Assert**
   - Arrange: Set up test data
   - Act: Make HTTP request
   - Assert: Check response and database

4. **Use Fresh Data**
   - Don't share test data between tests
   - Each test calls `Seed` if needed

5. **Test Both Success and Failure**
   - For each feature, write positive AND negative tests
   - Covers edge cases and error paths

6. **Verify Database State**
   - Don't just check HTTP response
   - Also verify data was actually persisted
   - Check audit logs were created

---

## Future Test Scenarios

- [ ] Password reset flow (forgot password → email → reset → login with new password)
- [ ] Role assignment (assign TeamAdmin to user → verify permissions)
- [ ] Team player management (TeamAdmin adding/removing players)
- [ ] Match result creation (DivisionAdmin creating match → teams updated)
- [ ] Permission cascading (remove user from division → teams still managed by other division admins)
- [ ] Concurrent access (multiple users modifying same team simultaneously)
- [ ] Large data sets (performance test with 1000+ teams)

---

## Resources

- [Microsoft.AspNetCore.Mvc.Testing Documentation](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [xUnit Fixtures](https://xunit.net/docs/shared-context)
- [FluentAssertions API](https://fluentassertions.com/)
