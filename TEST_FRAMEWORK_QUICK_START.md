# Testing Framework Quick Start

Complete testing setup for iDiski authentication with **50 total tests** across unit and integration suites.

---

## 🚀 Quick Commands

### Run All Tests
```bash
# Both unit and integration tests
dotnet test --configuration Release

# Just unit tests (27 tests)
dotnet test iDiski.Tests.Unit --configuration Release

# Just integration tests (23 tests)
dotnet test iDiski.Tests.Integration --configuration Release
```

### Run Specific Suite
```bash
# Authentication flow tests
dotnet test iDiski.Tests.Integration --filter "FullyQualifiedName~LoginIntegrationTests"

# Team CRUD tests
dotnet test iDiski.Tests.Integration --filter "FullyQualifiedName~TeamCrudIntegrationTests"

# User management tests
dotnet test iDiski.Tests.Integration --filter "FullyQualifiedName~UserManagementIntegrationTests"
```

### With Shell Script
```bash
chmod +x run-integration-tests.sh
./run-integration-tests.sh
```

---

## 📊 Test Coverage Overview

### Unit Tests (27 tests)
- **Authentication**: 4 tests (valid login, invalid password, non-existent user, inactive user)
- **Authorization**: 3 tests (SuperAdmin access, TeamAdmin restricted, unauthorized)
- **User Management**: 5 tests (create user, duplicate email, weak password, non-SuperAdmin, invalid email)
- **Team Operations**: 5 tests (SuperAdmin update, TeamAdmin update, unauthorized, not found, DivisionAdmin)
- **Audit Logging**: 7 tests (no filters, filter by type/user/entity, date range, pagination, no matches)

**Purpose**: Test isolated business logic with mocked dependencies

### Integration Tests (23 tests)
- **Authentication Flow**: 6 tests (login, JWT generation, token expiry, role claims, audit trail)
- **Team CRUD**: 7 tests (public read, SuperAdmin/TeamAdmin update, authorization checks, audit logs)
- **User Management**: 10 tests (create, duplicate email, weak password, SuperAdmin enforcement, listing)

**Purpose**: Test end-to-end flows with real HTTP, database, and middleware

---

## 🎯 What Each Type Validates

### Unit Tests (Mocked Dependencies)
✅ Password hashing logic works  
✅ Authorization decision logic works  
✅ CQRS handler business rules work  
✅ Validators catch invalid input  

### Integration Tests (Real HTTP + DB)
✅ JWT is generated and can be parsed  
✅ Unauthenticated requests get 401  
✅ Wrong role gets 403  
✅ Data is persisted to database  
✅ Audit logs are created  
✅ Proper HTTP status codes returned  

---

## 📁 Test Projects

### Unit Tests: `iDiski.Tests.Unit/`
```
iDiski.Tests.Unit/
├── Common/
│   ├── BaseTest.cs         (Test lifecycle)
│   └── TestFixture.cs      (Serilog setup)
├── Authentication/
│   └── LoginCommandTests.cs (4 tests)
├── Authorization/
│   └── TeamOwnershipHandlerTests.cs (3 tests)
├── Users/
│   └── CreateUserCommandTests.cs (5 tests)
├── Teams/
│   └── UpdateTeamCommandTests.cs (5 tests)
└── AuditLogs/
    └── GetAuditLogsQueryTests.cs (7 tests)
```

### Integration Tests: `iDiski.Tests.Integration/`
```
iDiski.Tests.Integration/
├── Common/
│   ├── IntegrationTestFixture.cs  (WebApplicationFactory)
│   └── TestDataSeeder.cs          (Seeds test data)
├── Authentication/
│   └── LoginIntegrationTests.cs (6 tests)
├── Teams/
│   └── TeamCrudIntegrationTests.cs (7 tests)
└── Users/
    └── UserManagementIntegrationTests.cs (10 tests)
```

---

## 🔧 How to Add New Tests

### Add Unit Test
1. Create file: `iDiski.Tests.Unit/Feature/YourTests.cs`
2. Inherit `BaseTest` fixture
3. Write tests using xUnit `[Fact]` attribute
4. Use `Moq` to mock dependencies
5. Use `FluentAssertions` for assertions

```csharp
[Fact]
public void YourFeature_WithCondition_ExpectedBehavior()
{
    // Arrange
    var mockDb = new Mock<ILeagueDbContext>();
    
    // Act
    var result = handler.Handle(request);
    
    // Assert
    result.Should().Be(expected);
}
```

### Add Integration Test
1. Create file: `iDiski.Tests.Integration/Feature/YourTests.cs`
2. Inherit `IClassFixture<IntegrationTestFixture>`
3. Use `_fixture.HttpClient` for requests
4. Use `_fixture.DbContext` for database assertions
5. Get JWT via `GetTokenAsync()` helper

```csharp
[Fact]
public async Task YourFeature_WithCondition_ReturnsExpected()
{
    // Arrange
    await TestDataSeeder.SeedTestUsersAsync(_fixture.DbContext);
    var token = await GetSuperAdminTokenAsync();
    _fixture.HttpClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await _fixture.HttpClient.GetAsync("/api/endpoint");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var data = await response.Content.ReadAsAsync<MyDto>();
    data.Should().NotBeNull();
}
```

---

## 🔐 Security Tests Covered

✅ **Authentication**
- Valid credentials return JWT token
- Invalid password returns 401
- Non-existent user returns 401
- Inactive user returns 403
- JWT includes roles

✅ **Authorization**
- SuperAdmin bypasses checks
- TeamAdmin limited to assigned teams
- DivisionAdmin limited to assigned division
- Unauthenticated users get 401
- Wrong role gets 403

✅ **Data Validation**
- Email uniqueness enforced
- Password complexity validated
- Required fields checked
- Invalid IDs return 404

✅ **Audit Trail**
- Changes logged with CreatedByUserId/UpdatedByUserId
- Old/new values stored as JSON
- Queryable by SuperAdmin
- Timestamps accurate

---

## 📈 CI/CD Integration

### GitHub Actions
```yaml
- name: Run all tests
  run: dotnet test --configuration Release

- name: Run integration tests only
  run: dotnet test iDiski.Tests.Integration
```

### Pre-commit Hook
```bash
#!/bin/sh
dotnet test --configuration Release || exit 1
```

### Before Deployment
```bash
# Full test suite
dotnet test

# Verbose output to see what's being tested
dotnet test --verbosity detailed

# With code coverage report
dotnet test /p:CollectCoverage=true
```

---

## 🐛 Debugging Tests

### Run Single Test
```bash
dotnet test --filter "YourTests.YourTest"
```

### See Test Output
```bash
dotnet test --verbosity detailed
```

### Break in Debugger
```csharp
// Add breakpoint and run with debugger
dotnet test --configuration Debug
```

### Print Debug Info
```csharp
using Serilog;

ILogger _logger = Log.ForContext<YourTest>();

[Fact]
public void Test()
{
    _logger.Information("Debug info: {@value}", data);
    // ... test code
}
```

---

## 📚 Documentation

- **INTEGRATION_TESTING.md** - Comprehensive guide for integration tests
- **test-reports/index.html** - Unit test report
- **test-reports/integration.html** - Integration test report
- **test-reports/INTEGRATION_TESTS_SUMMARY.md** - Overview

---

## ✅ Test Quality Checklist

Before pushing, verify:

- [ ] All tests pass locally: `dotnet test`
- [ ] Tests run in Release config: `dotnet test --configuration Release`
- [ ] Both positive and negative tests written
- [ ] Database state verified (not just HTTP response)
- [ ] Descriptive test names
- [ ] No hardcoded test IDs (use seeded data)
- [ ] Tests clean up after themselves
- [ ] Audit trail verified for data changes

---

## 🚨 Common Issues

**Tests fail with "Database is locked"**
- Fixture creates fresh DB for each test, should not happen
- Check for missing `await` on async operations

**Tests pass locally but fail on CI**
- Check timezone issues (use DateTime.UtcNow)
- Verify test data seeding is consistent

**JWT token not sent with request**
```csharp
// WRONG: Request has no Authorization header
var response = await _fixture.HttpClient.GetAsync("/api/users");

// RIGHT: Set Authorization header
var token = await GetTokenAsync();
_fixture.HttpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);
var response = await _fixture.HttpClient.GetAsync("/api/users");
```

---

## 🎯 Test Results

Current coverage:
- **50 total tests** (27 unit + 23 integration)
- **13 positive tests** (happy path)
- **10 negative tests** (error cases)
- **3 authorization test suites**
- **7 audit trail tests**

Run tests to verify everything works:
```bash
dotnet test
```

All tests should pass ✅
