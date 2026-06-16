using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;

namespace iDiski.Tests.Unit.Common;

public class TestFixture
{
    private readonly string _logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
    private readonly Logger _logger;

    public TestFixture()
    {
        // Create logs directory if it doesn't exist
        Directory.CreateDirectory(_logsDirectory);

        // Configure Serilog for test logging
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(_logsDirectory, "test-run-.jsonl"),
                outputTemplate: "{Message:j}{NewLine}",
                rollingInterval: RollingInterval.Day,
                shared: true)
            .WriteTo.Console()
            .Enrich.WithProperty("Environment", "Test")
            .CreateLogger();
    }

    public ILogger Logger => _logger;
    public string LogsDirectory => _logsDirectory;

    public void LogTestStart(string testName)
    {
        _logger.Information("TEST_START {@Test}", new { Name = testName, StartTime = DateTime.UtcNow });
    }

    public void LogTestPass(string testName, TimeSpan duration)
    {
        _logger.Information("TEST_PASS {@Test}", new { Name = testName, Duration = duration.TotalMilliseconds, Status = "PASSED" });
    }

    public void LogTestFail(string testName, string error, TimeSpan duration)
    {
        _logger.Error("TEST_FAIL {@Test}", new { Name = testName, Error = error, Duration = duration.TotalMilliseconds, Status = "FAILED" });
    }

    public void Dispose()
    {
        _logger?.Dispose();
    }
}
