# iDiski Testing Guide

## Overview

The iDiski project includes a comprehensive automated test suite with structured logging and reporting.

- **Test Framework**: xUnit
- **Mocking**: Moq
- **Assertions**: FluentAssertions
- **Logging**: Serilog (JSON structured logs)
- **Reports**: HTML + JSON

## Quick Start

### Windows (PowerShell)

```powershell
# Run all tests with report generation
.\run-tests.ps1

# Run with verbose output
.\run-tests.ps1 -Verbose
```

### macOS / Linux (Bash)

```bash
# Make script executable
chmod +x run-tests.sh

# Run all tests with report generation
./run-tests.sh
```

### Manual (dotnet CLI)

```bash
cd iDiski.Tests.Unit
dotnet test --verbosity minimal
```

## Test Structure

### Unit Tests (`iDiski.Tests.Unit`)

Located in `iDiski.Tests.Unit/` with the following organization:

```
iDiski.Tests.Unit/
├── Authentication/
│   └── LoginCommandTests.cs
├── Authorization/
│   └── TeamOwnershipHandlerTests.cs
├── Common/
│   ├── BaseTest.cs          # Base class for all tests
│   └── TestFixture.cs       # Test infrastructure
```

## Test Reports

After running tests, reports are generated in the `test-reports/` directory:

### Report Files

- **HTML Report**: `test-report-YYYY-MM-DD_HHMMSS.html`
  - Visual dashboard with pass/fail statistics
  - Open in any web browser

- **JSON Report**: `test-report-YYYY-MM-DD_HHMMSS.json`
  - Machine-readable results
  - For CI/CD pipeline integration

- **Structured Logs**: `logs/test-run-*.jsonl`
  - Line-delimited JSON format
  - Each line is a complete log entry
  - Queryable with tools like `jq`

### Example: Reading Test Logs

```bash
# View all test failures
cat test-reports/logs/test-run-*.jsonl | grep "TEST_FAIL"

# Count passed tests
cat test-reports/logs/test-run-*.jsonl | grep "TEST_PASS" | wc -l

# Filter by test name (using jq)
cat test-reports/logs/test-run-*.jsonl | jq 'select(.Test.Name == "LoginCommandTests")'
```

## Test Categories

### Authentication Tests

**File**: `iDiski.Tests.Unit/Authentication/LoginCommandTests.cs`

Tests the login flow with various scenarios:

- ✅ Valid credentials → Returns JWT token
- ❌ Invalid password → Throws UnauthorizedException
- ❌ Nonexistent user → Throws UnauthorizedException
- ❌ Inactive user → Throws UnauthorizedException

Run only authentication tests:

```bash
dotnet test --filter "Category=Authentication"
```

### Authorization Tests

**File**: `iDiski.Tests.Unit/Authorization/TeamOwnershipHandlerTests.cs`

Tests role-based access control:

- ✅ Super Admin → Full access (no assignment needed)
- ✅ Team Admin → Access only to assigned team
- ❌ Unassigned user → Forbidden
- ✅ Division Admin → Access to teams in assigned division

Run only authorization tests:

```bash
dotnet test --filter "Category=Authorization"
```

## Writing New Tests

### Test Template

```csharp
using Xunit;
using Moq;
using FluentAssertions;
using iDiski.Tests.Unit.Common;

namespace iDiski.Tests.Unit.YourFeature;

public class YourFeatureTests : BaseTest
{
    [Fact]
    public async Task YourMethod_WithScenario_ExpectedResult()
    {
        // Arrange
        var testName = "YourMethod_WithScenario_ExpectedResult";
        LogTestStart(testName);

        // Setup mocks
        var mockDependency = new Mock<IDependency>();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(expected);

        LogTestPass(testName);
    }
}
```

### Testing Patterns

#### 1. Mocking DbSets

```csharp
var mockUsers = new Mock<DbSet<User>>();
mockUsers
    .Setup(x => x.FirstOrDefaultAsync(
        It.IsAny<Expression<Func<User, bool>>>(),
        It.IsAny<CancellationToken>()))
    .ReturnsAsync(user);

dbContext.Setup(x => x.Users).Returns(mockUsers.Object);
```

#### 2. Assertion Examples

```csharp
// String assertions
result.Email.Should().Be("admin@test.com");
result.Email.Should().Contain("@");

// Numeric assertions
result.Age.Should().BeGreaterThan(18);
result.Count.Should().Be(5);

// Collection assertions
result.Roles.Should().HaveCount(2);
result.Roles.Should().Contain("SuperAdmin");

// Exception assertions
await Assert.ThrowsAsync<UnauthorizedException>(
    () => handler.Handle(command, CancellationToken.None)
);
```

#### 3. Async Test Pattern

```csharp
[Fact]
public async Task AsyncMethod_Returns_ExpectedValue()
{
    // Arrange
    LogTestStart(nameof(AsyncMethod_Returns_ExpectedValue));

    // Act
    var result = await service.GetDataAsync();

    // Assert
    result.Should().NotBeNull();
    
    LogTestPass(nameof(AsyncMethod_Returns_ExpectedValue));
}
```

## CI/CD Integration

### GitHub Actions

Add to `.github/workflows/tests.yml`:

```yaml
name: Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      - run: dotnet restore
      - run: dotnet build
      - run: ./run-tests.sh
      - uses: actions/upload-artifact@v3
        if: always()
        with:
          name: test-reports
          path: test-reports/
```

## Test Coverage Goals

| Layer | Coverage | Status |
|-------|----------|--------|
| Domain Services | 100% | ✅ |
| Application (CQRS) | 80%+ | 🚀 |
| Authorization | 95%+ | 🚀 |
| API Controllers | 70%+ | 📋 |
| Infrastructure | 60%+ | 📋 |

## Troubleshooting

### Tests not finding mocks

Ensure mock setup matches the exact expression:

```csharp
// ❌ Wrong - lambda mismatch
mockUsers.Setup(x => x.FirstOrDefaultAsync(
    It.IsAny<Expression<Func<User, bool>>>(),
    CancellationToken.None))

// ✅ Correct - use It.IsAny<CancellationToken>()
mockUsers.Setup(x => x.FirstOrDefaultAsync(
    It.IsAny<Expression<Func<User, bool>>>(),
    It.IsAny<CancellationToken>()))
```

### Async test hangs

Ensure CancellationToken.None is passed:

```csharp
// ✅ Correct
var result = await handler.Handle(command, CancellationToken.None);
```

### Reports not generating

Check that xUnit logger is configured:

```bash
dotnet test --logger "json;LogFileName=report.json"
```

## Performance Testing

For performance-critical operations:

```csharp
[Fact]
public async Task UpdateTeam_PerformanceTest()
{
    var stopwatch = Stopwatch.StartNew();

    await handler.Handle(command, CancellationToken.None);

    stopwatch.Stop();
    stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(500));
}
```

## Running Tests Regularly

### Local Development

Run tests after every significant change:

```bash
# Run all tests
dotnet test

# Run specific test file
dotnet test --filter "FullyQualifiedName~LoginCommandTests"

# Run with specific trait
dotnet test --filter "Category=Authentication"
```

### Before Committing

```bash
# Full test suite with report
./run-tests.ps1  # Windows
./run-tests.sh   # macOS/Linux
```

## References

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Serilog Documentation](https://serilog.net/)

