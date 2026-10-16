using System.Diagnostics;
using ILogger = Serilog.ILogger;

namespace ERP.Application.Common.Behaviors;

/// <summary>
/// Logs the start and the outcome of every request, with how long it took.
/// <para>
/// The request itself is logged as a structured property rather than formatted into the message,
/// so the Serilog enrichers still see it - which is what keeps a password out of the log even
/// though the sign-up command carries one.
/// </para>
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>Anything slower than this is worth noticing even when it succeeded.</summary>
    private const int SlowRequestMilliseconds = 500;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        logger.Information("Handling {RequestName} {@Request}", requestName, request);

        try
        {
            var response = await next(cancellationToken);
            stopwatch.Stop();

            // A business failure is an expected answer, not an incident, so it is recorded at
            // warning rather than error - otherwise "username already taken" would page somebody.
            // The whole result is logged rather than just its error: the non generic IResult
            // only exposes whether it failed, and the error lives on the closed generic.
            if (response is IResult { IsFailure: true })
            {
                logger.Warning(
                    "{RequestName} refused in {ElapsedMilliseconds} ms: {@Response}",
                    requestName,
                    stopwatch.ElapsedMilliseconds,
                    response);

                return response;
            }

            if (stopwatch.ElapsedMilliseconds >= SlowRequestMilliseconds)
            {
                logger.Warning("{RequestName} was slow: {ElapsedMilliseconds} ms", requestName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                logger.Information("{RequestName} handled in {ElapsedMilliseconds} ms", requestName, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            logger.Error(exception, "{RequestName} threw after {ElapsedMilliseconds} ms", requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
