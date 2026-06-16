#!/bin/bash

# iDiski Test Runner Script
# Runs all tests and generates HTML/JSON reports

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

# Paths
SOLUTION_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TEST_PROJECT_PATH="$SOLUTION_ROOT/iDiski.Tests.Unit"
REPORT_DIR="$SOLUTION_ROOT/test-reports"
LOGS_DIR="$REPORT_DIR/logs"
TIMESTAMP=$(date +"%Y-%m-%d_%H%M%S")
REPORT_FILE="$REPORT_DIR/test-report-$TIMESTAMP.json"
HTML_REPORT_FILE="$REPORT_DIR/test-report-$TIMESTAMP.html"

# Create directories
mkdir -p "$REPORT_DIR" "$LOGS_DIR"

echo -e "${CYAN}🧪 Running iDiski Test Suite${NC}"
echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GRAY}Report Directory: $REPORT_DIR${NC}"
echo -e "${GRAY}Timestamp: $TIMESTAMP${NC}"
echo ""

# Run tests
echo -e "${YELLOW}▶ Executing tests...${NC}"
dotnet test \
    "$TEST_PROJECT_PATH" \
    --no-build \
    --verbosity minimal \
    --logger "json;LogFileName=$REPORT_FILE" || TEST_EXIT_CODE=$?

# Parse JSON results if available
if [ -f "$REPORT_FILE" ]; then
    TOTAL_TESTS=$(grep -o '"tests":[0-9]*' "$REPORT_FILE" | grep -o '[0-9]*')
    PASSED_TESTS=$(grep -o '"passed":[0-9]*' "$REPORT_FILE" | grep -o '[0-9]*')
    FAILED_TESTS=$(grep -o '"failed":[0-9]*' "$REPORT_FILE" | grep -o '[0-9]*')
    SKIPPED_TESTS=$(grep -o '"skipped":[0-9]*' "$REPORT_FILE" | grep -o '[0-9]*')
    DURATION=$(grep -o '"duration":"[^"]*' "$REPORT_FILE" | grep -o '[0-9]*')

    # Display summary
    echo ""
    echo -e "${CYAN}📊 Test Results Summary${NC}"
    echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "Total Tests:    ${GRAY}$TOTAL_TESTS${NC}"
    echo -e "✅ Passed:      ${GREEN}$PASSED_TESTS${NC}"

    if [ "$FAILED_TESTS" -gt 0 ]; then
        echo -e "❌ Failed:      ${RED}$FAILED_TESTS${NC}"
    else
        echo -e "❌ Failed:      ${GREEN}$FAILED_TESTS${NC}"
    fi

    echo -e "⏭️  Skipped:     ${YELLOW}$SKIPPED_TESTS${NC}"
    echo -e "⏱️  Duration:    ${GRAY}${DURATION}ms${NC}"
    echo ""

    # Generate HTML report
    PASS_RATE=0
    if [ "$TOTAL_TESTS" -gt 0 ]; then
        PASS_RATE=$((PASSED_TESTS * 100 / TOTAL_TESTS))
    fi

    cat > "$HTML_REPORT_FILE" <<EOF
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>iDiski Test Report - $TIMESTAMP</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; background: #f5f5f5; padding: 20px; }
        .container { max-width: 1000px; margin: 0 auto; background: white; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); padding: 30px; }
        h1 { color: #333; margin-bottom: 10px; font-size: 28px; }
        .subtitle { color: #666; margin-bottom: 20px; font-size: 14px; }
        .stats { display: grid; grid-template-columns: repeat(4, 1fr); gap: 15px; margin-bottom: 30px; }
        .stat { background: #f9f9f9; padding: 15px; border-radius: 6px; border-left: 4px solid #ddd; }
        .stat.total { border-left-color: #2563eb; }
        .stat.passed { border-left-color: #16a34a; }
        .stat.failed { border-left-color: #dc2626; }
        .stat.skipped { border-left-color: #f59e0b; }
        .stat-value { font-size: 24px; font-weight: bold; color: #333; }
        .stat-label { font-size: 12px; color: #666; margin-top: 5px; text-transform: uppercase; }
        .summary { background: #f0f9ff; border-left: 4px solid #2563eb; padding: 15px; margin-bottom: 20px; border-radius: 4px; }
        .summary p { color: #1e40af; margin: 5px 0; }
        .footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #e5e5e5; color: #999; font-size: 12px; }
    </style>
</head>
<body>
    <div class="container">
        <h1>🧪 iDiski Test Report</h1>
        <div class="subtitle">Generated on $TIMESTAMP</div>

        <div class="stats">
            <div class="stat total">
                <div class="stat-value">$TOTAL_TESTS</div>
                <div class="stat-label">Total Tests</div>
            </div>
            <div class="stat passed">
                <div class="stat-value">$PASSED_TESTS</div>
                <div class="stat-label">Passed</div>
            </div>
            <div class="stat failed">
                <div class="stat-value">$FAILED_TESTS</div>
                <div class="stat-label">Failed</div>
            </div>
            <div class="stat skipped">
                <div class="stat-value">$SKIPPED_TESTS</div>
                <div class="stat-label">Skipped</div>
            </div>
        </div>

        <div class="summary">
            <p><strong>Pass Rate:</strong> $PASS_RATE%</p>
            <p><strong>Duration:</strong> $(echo "scale=2; $DURATION / 1000" | bc)s</p>
            <p><strong>Status:</strong> $(if [ "$FAILED_TESTS" -eq 0 ]; then echo "✅ ALL TESTS PASSED"; else echo "❌ SOME TESTS FAILED"; fi)</p>
        </div>

        <div class="footer">
            <p>Reports location: $REPORT_DIR</p>
            <p>JSON Report: test-report-$TIMESTAMP.json</p>
            <p>Logs location: $LOGS_DIR</p>
        </div>
    </div>
</body>
</html>
EOF

    echo -e "${BLUE}📄 HTML Report: test-report-$TIMESTAMP.html${NC}"
    echo -e "${BLUE}📋 JSON Report: test-report-$TIMESTAMP.json${NC}"
fi

# Display failed tests
if [ "$FAILED_TESTS" -gt 0 ]; then
    echo ""
    echo -e "${RED}❌ Failed Tests:${NC}"
    echo -e "${RED}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    # Note: Actual failed tests would be shown from the test output
fi

echo ""
if [ "${TEST_EXIT_CODE:-0}" -eq 0 ]; then
    echo -e "${GREEN}✅ All tests passed!${NC}"
else
    echo -e "${RED}❌ Some tests failed. Check the report for details.${NC}"
fi

exit "${TEST_EXIT_CODE:-0}"
