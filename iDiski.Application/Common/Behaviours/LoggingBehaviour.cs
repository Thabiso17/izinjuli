using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace iDiski.Application.Common.Behaviours;

/// <summary>
/// Wraps every MediatR request with entry/exit logging and elapsed-time measurement.
/// Logs a warning for requests that exceed 500 ms so slow queries are easy to spot.
/// </summary>
public sealed class LoggingBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 500;

    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("iDiski → Handling {RequestName}", requestName);

        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > SlowRequestThresholdMs)
            _logger.LogWarning(
                "iDiski ⚠ Slow request: {RequestName} took {Elapsed}ms",
                requestName, sw.ElapsedMilliseconds);
        else
            _logger.LogInformation(
                "iDiski ✓ Handled {RequestName} in {Elapsed}ms",
                requestName, sw.ElapsedMilliseconds);

        return response;
    }
}
