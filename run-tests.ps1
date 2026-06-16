#Requires -Version 5.0

<#
.SYNOPSIS
    Runs all iDiski tests and generates a comprehensive report
.DESCRIPTION
    Executes xUnit tests, captures results, and generates HTML/JSON reports
.EXAMPLE
    .\run-tests.ps1
    .\run-tests.ps1 -Verbose
#>

param(
    [switch]$Verbose = $false
)

$ErrorActionPreference = "Stop"

# Define paths
$solutionRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$testProjectPath = Join-Path $solutionRoot "iDiski.Tests.Unit"
$reportDir = Join-Path $solutionRoot "test-reports"
$logsDir = Join-Path $reportDir "logs"
$timestamp = Get-Date -Format "yyyy-MM-dd_HHmmss"
$reportFile = Join-Path $reportDir "test-report-$timestamp.json"
$htmlReportFile = Join-Path $reportDir "test-report-$timestamp.html"

# Create directories
New-Item -ItemType Directory -Force -Path $reportDir, $logsDir | Out-Null

Write-Host "🧪 Running iDiski Test Suite" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "Report Directory: $reportDir" -ForegroundColor Gray
Write-Host "Timestamp: $timestamp" -ForegroundColor Gray
Write-Host ""

# Run tests
Write-Host "▶ Executing tests..." -ForegroundColor Yellow
$testOutput = & dotnet test `
    $testProjectPath `
    --no-build `
    --verbosity minimal `
    --logger "json;LogFileName=$reportFile" `
    2>&1

$testExitCode = $LASTEXITCODE

# Parse results
if (Test-Path $reportFile) {
    $jsonReport = Get-Content $reportFile | ConvertFrom-Json

    $totalTests = $jsonReport.tests
    $passedTests = $jsonReport.passed
    $failedTests = $jsonReport.failed
    $skippedTests = $jsonReport.skipped
    $duration = $jsonReport.duration

    # Display summary
    Write-Host ""
    Write-Host "📊 Test Results Summary" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host "Total Tests:    $totalTests" -ForegroundColor White
    Write-Host "✅ Passed:      $passedTests" -ForegroundColor Green
    Write-Host "❌ Failed:      $failedTests" -ForegroundColor $(if ($failedTests -gt 0) { "Red" } else { "Green" })
    Write-Host "⏭️  Skipped:     $skippedTests" -ForegroundColor Yellow
    Write-Host "⏱️  Duration:    ${duration}ms" -ForegroundColor Gray
    Write-Host ""

    # Generate HTML report
    $htmlContent = @"
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>iDiski Test Report - $timestamp</title>
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
        <div class="subtitle">Generated on $timestamp</div>

        <div class="stats">
            <div class="stat total">
                <div class="stat-value">$totalTests</div>
                <div class="stat-label">Total Tests</div>
            </div>
            <div class="stat passed">
                <div class="stat-value">$passedTests</div>
                <div class="stat-label">Passed</div>
            </div>
            <div class="stat failed">
                <div class="stat-value">$failedTests</div>
                <div class="stat-label">Failed</div>
            </div>
            <div class="stat skipped">
                <div class="stat-value">$skippedTests</div>
                <div class="stat-label">Skipped</div>
            </div>
        </div>

        <div class="summary">
            <p><strong>Pass Rate:</strong> $(if ($totalTests -gt 0) { [math]::Round(($passedTests / $totalTests) * 100, 2) } else { 0 })%</p>
            <p><strong>Duration:</strong> $([math]::Round($duration / 1000, 2))s</p>
            <p><strong>Status:</strong> $(if ($failedTests -eq 0) { "✅ ALL TESTS PASSED" } else { "❌ SOME TESTS FAILED" })</p>
        </div>

        <div class="footer">
            <p>Reports location: $reportDir</p>
            <p>JSON Report: test-report-$timestamp.json</p>
            <p>Logs location: $logsDir</p>
        </div>
    </div>
</body>
</html>
"@

    $htmlContent | Out-File -FilePath $htmlReportFile -Encoding UTF8
    Write-Host "📄 HTML Report: test-report-$timestamp.html" -ForegroundColor Blue
    Write-Host "📋 JSON Report: test-report-$timestamp.json" -ForegroundColor Blue
}

# Display any failed tests
if ($failedTests -gt 0) {
    Write-Host ""
    Write-Host "❌ Failed Tests:" -ForegroundColor Red
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Red
    $testOutput | Select-String "FAILED" | ForEach-Object { Write-Host $_ -ForegroundColor Red }
}

# Copy test logs to report directory
if (Test-Path (Join-Path $testProjectPath "bin/Debug/net9.0/logs")) {
    Copy-Item -Path (Join-Path $testProjectPath "bin/Debug/net9.0/logs/*") -Destination $logsDir -Force -Recurse -ErrorAction SilentlyContinue
    Write-Host ""
    Write-Host "📁 Test logs copied to: $logsDir" -ForegroundColor Gray
}

# Exit with test exit code
Write-Host ""
if ($testExitCode -eq 0) {
    Write-Host "✅ All tests passed!" -ForegroundColor Green
} else {
    Write-Host "❌ Some tests failed. Check the report for details." -ForegroundColor Red
}

exit $testExitCode
