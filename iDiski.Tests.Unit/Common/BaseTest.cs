using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.Diagnostics;

namespace iDiski.Tests.Unit.Common;

public abstract class BaseTest : IDisposable
{
    protected readonly TestFixture Fixture;
    private readonly Stopwatch _stopwatch;

    protected BaseTest()
    {
        Fixture = new TestFixture();
        _stopwatch = Stopwatch.StartNew();
    }

    protected void LogTestStart(string testName)
    {
        Fixture.LogTestStart(testName);
    }

    protected void LogTestPass(string testName)
    {
        _stopwatch.Stop();
        Fixture.LogTestPass(testName, _stopwatch.Elapsed);
    }

    protected void LogTestFail(string testName, string error)
    {
        _stopwatch.Stop();
        Fixture.LogTestFail(testName, error, _stopwatch.Elapsed);
    }

    public virtual void Dispose()
    {
        Fixture?.Dispose();
    }
}
