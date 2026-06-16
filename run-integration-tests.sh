#!/bin/bash

# iDiski Integration Test Runner
# Runs integration tests and generates HTML/JSON reports

set -e

REPORT_DIR="test-reports"
TIMESTAMP=$(date +%Y-%m-%d_%H%M%S)
REPORT_NAME="integration-tests-$TIMESTAMP"

echo -e "\033[0;36m🧪 Running iDiski Integration Tests\033[0m"
echo -e "\033[0;36m━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\033[0m"
echo -e "\033[0;90mReport Directory: $(pwd)/$REPORT_DIR\033[0m"
echo -e "\033[0;90mTimestamp: $TIMESTAMP\033[0m"
echo ""

# Create report directory
mkdir -p "$REPORT_DIR"

# Run integration tests
echo -e "\033[1;33m▶ Executing integration tests...\033[0m"

# Run tests with TRX output
dotnet test iDiski.Tests.Integration \
    --configuration Release \
    --logger "trx;LogFileName=$REPORT_DIR/$REPORT_NAME.trx" \
    --logger "console;verbosity=minimal" \
    --verbosity minimal \
    2>&1 | tee "$REPORT_DIR/test-output.log"

TEST_EXIT_CODE=$?

# Count passed/failed from log
PASSED_COUNT=$(grep -c "PASSED" "$REPORT_DIR/test-output.log" || echo 0)
FAILED_COUNT=$(grep -c "FAILED" "$REPORT_DIR/test-output.log" || echo 0)

if [ $TEST_EXIT_CODE -eq 0 ]; then
    echo -e "\033[0;32m✅ All integration tests passed!\033[0m"
else
    echo -e "\033[0;31m❌ Some integration tests failed.\033[0m"
fi

# Generate JSON report
cat > "$REPORT_DIR/integration-test-results.json" << EOF
{
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "testRun": "Integration Tests",
  "configuration": "Release",
  "summary": {
    "passed": $PASSED_COUNT,
    "failed": $FAILED_COUNT,
    "total": $((PASSED_COUNT + FAILED_COUNT)),
    "exitCode": $TEST_EXIT_CODE
  },
  "suites": [
    {
      "name": "Authentication",
      "tests": [
        {
          "name": "Login_WithValidCredentials_ReturnsJwtToken",
          "type": "POSITIVE",
          "description": "Valid credentials return JWT token with user info"
        },
        {
          "name": "Login_WithInvalidPassword_Returns401Unauthorized",
          "type": "NEGATIVE",
          "description": "Invalid password returns 401 Unauthorized"
        },
        {
          "name": "Login_WithNonExistentEmail_Returns401Unauthorized",
          "type": "NEGATIVE",
          "description": "Non-existent email returns 401 Unauthorized"
        },
        {
          "name": "Login_WithInactiveUser_Returns403Forbidden",
          "type": "NEGATIVE",
          "description": "Inactive user returns 403 Forbidden"
        },
        {
          "name": "Login_UpdatesLastLoginAt",
          "type": "POSITIVE",
          "description": "Login updates user's LastLoginAt timestamp"
        },
        {
          "name": "Login_IncludesUserRolesInToken",
          "type": "POSITIVE",
          "description": "JWT token includes user roles in claims"
        }
      ]
    },
    {
      "name": "Team CRUD Operations",
      "tests": [
        {
          "name": "GetTeams_ReturnsAllTeams_WithoutAuthentication",
          "type": "POSITIVE",
          "description": "Public endpoint returns all teams without auth"
        },
        {
          "name": "UpdateTeam_AsSuperAdmin_Succeeds",
          "type": "POSITIVE",
          "description": "SuperAdmin can update any team"
        },
        {
          "name": "UpdateTeam_AsTeamAdmin_OnAssignedTeam_Succeeds",
          "type": "POSITIVE",
          "description": "TeamAdmin can update assigned team"
        },
        {
          "name": "UpdateTeam_AsTeamAdmin_OnUnassignedTeam_Returns403",
          "type": "NEGATIVE",
          "description": "TeamAdmin cannot update unassigned team"
        },
        {
          "name": "UpdateTeam_WithoutAuthentication_Returns401",
          "type": "NEGATIVE",
          "description": "Protected endpoint requires authentication"
        },
        {
          "name": "UpdateTeam_NonExistentTeam_Returns404",
          "type": "NEGATIVE",
          "description": "Updating non-existent team returns 404"
        },
        {
          "name": "UpdateTeam_TracksAuditLog",
          "type": "POSITIVE",
          "description": "Team updates are recorded in audit log"
        }
      ]
    },
    {
      "name": "User Management",
      "tests": [
        {
          "name": "CreateUser_AsSuperAdmin_Succeeds",
          "type": "POSITIVE",
          "description": "SuperAdmin can create new user with valid data"
        },
        {
          "name": "CreateUser_WithDuplicateEmail_Returns422",
          "type": "NEGATIVE",
          "description": "Cannot create user with duplicate email"
        },
        {
          "name": "CreateUser_WithWeakPassword_Returns422",
          "type": "NEGATIVE",
          "description": "Password must meet complexity requirements"
        },
        {
          "name": "CreateUser_AsNonSuperAdmin_Returns403",
          "type": "NEGATIVE",
          "description": "Only SuperAdmin can create users"
        },
        {
          "name": "CreateUser_WithoutAuthentication_Returns401",
          "type": "NEGATIVE",
          "description": "User creation requires authentication"
        },
        {
          "name": "GetCurrentUser_ReturnsAuthenticatedUser",
          "type": "POSITIVE",
          "description": "Authenticated user can retrieve their profile"
        },
        {
          "name": "GetCurrentUser_WithoutAuthentication_Returns401",
          "type": "NEGATIVE",
          "description": "Anonymous request cannot access current user"
        },
        {
          "name": "GetUser_BySuperAdmin_ReturnsUserWithRoles",
          "type": "POSITIVE",
          "description": "SuperAdmin can retrieve any user with roles"
        },
        {
          "name": "ListUsers_BySuperAdmin_ReturnsAllUsers",
          "type": "POSITIVE",
          "description": "SuperAdmin can list all users"
        },
        {
          "name": "ListUsers_AsNonSuperAdmin_Returns403",
          "type": "NEGATIVE",
          "description": "Non-SuperAdmin cannot list users"
        }
      ]
    }
  ]
}
EOF

echo ""
echo -e "\033[1;32m✨ Reports generated:\033[0m"
echo "  📊 HTML Report: $REPORT_DIR/index.html"
echo "  📋 JSON Report: $REPORT_DIR/integration-test-results.json"
echo "  📝 Test Output: $REPORT_DIR/test-output.log"
echo ""

exit $TEST_EXIT_CODE
