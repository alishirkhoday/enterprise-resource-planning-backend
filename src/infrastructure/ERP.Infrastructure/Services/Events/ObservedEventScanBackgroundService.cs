using ERP.Application.Common.Interfaces.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ERP.Infrastructure.Services.Events;

/// <summary>
/// Runs the daily pass over the books that finds cheques falling due, receivables going overdue
/// and the rest of the facts that arrive by the calendar rather than by anyone doing anything.
/// </summary>
public sealed class ObservedEventScanBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ObservedEventScanOptions> options,
    TimeProvider timeProvider,
    Serilog.ILogger logger) : BackgroundService
{
    private readonly ObservedEventScanOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.Information("The observed event scan is switched off by configuration.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var wait = TimeUntilNextRun();
            logger.Information("The next observed event scan is in {Hours:0.0} hours.", wait.TotalHours);

            try
            {
                await Task.Delay(wait, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var scanner = scope.ServiceProvider.GetRequiredService<IObservedEventScanner>();

                var asOf = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
                var result = await scanner.ScanAsync(asOf, stoppingToken);

                logger.Information("The observed event scan for {AsOf} published {Total} facts: {@Result}", asOf, result.Total, result);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Tomorrow's pass finds whatever today's missed, because these facts are derived
                // from the books rather than from an event that can be lost.
                logger.Error(exception, "The observed event scan failed. The next pass will pick it up.");
            }
        }

        logger.Information("The observed event scan has stopped.");
    }

    /// <summary>
    /// Time until the next configured hour. Anchored to a clock time rather than to when the
    /// process happened to start, so a restart does not quietly move the daily run to 3am.
    /// </summary>
    private TimeSpan TimeUntilNextRun()
    {
        var now = timeProvider.GetLocalNow();
        var next = new DateTimeOffset(now.Year, now.Month, now.Day, _options.RunAtHour, 0, 0, now.Offset);

        while (next <= now)
        {
            next = next.AddHours(_options.IntervalHours);
        }

        return next - now;
    }
}
