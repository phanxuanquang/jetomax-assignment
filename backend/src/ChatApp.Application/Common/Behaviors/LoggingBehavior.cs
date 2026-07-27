using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ChatApp.Application.Common.Behaviors;

/// <summary>
/// Logs entry, completion (with elapsed time), and any unhandled exception for every request that
/// passes through the pipeline. Adds no business behavior and never alters the response.
/// </summary>
/// <typeparam name="TRequest">The command or query being logged.</typeparam>
/// <typeparam name="TResponse">The handler's response type.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Logs around <paramref name="next"/> and rethrows any exception unchanged.</summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            var response = await next();
            logger.LogInformation("Handled {RequestName} in {ElapsedMilliseconds}ms", requestName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{RequestName} threw after {ElapsedMilliseconds}ms", requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
