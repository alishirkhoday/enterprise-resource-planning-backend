using ERP.Application.Common.Interfaces.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ERP.Infrastructure.Services.Events;

/// <summary>
/// Drains the outbox on a loop. Without something like this the outbox is a table that only ever
/// fills up, and every domain event the system records is stored and never delivered.
/// </summary>
public sealed class OutboxBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    Serilog.ILogger logger) : BackgroundService
{
    private readonly OutboxOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.Information("The outbox processor is switched off by configuration.");
            return;
        }

        logger.Information(
            "The outbox processor is running every {PollingIntervalSeconds}s in batches of {BatchSize}.",
            _options.PollingIntervalSeconds,
            _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = TimeSpan.FromSeconds(_options.PollingIntervalSeconds);

            try
            {
                // A scope per pass. The processor depends on a DbContext, which is scoped, and a
                // long lived one would accumulate every entity it ever tracked.
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

                var dispatched = await processor.ProcessPendingAsync(_options.BatchSize, stoppingToken);

                // A full batch means there is more waiting, so the next pass starts immediately
                // rather than sleeping through a backlog.
                if (dispatched >= _options.BatchSize)
                    continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // The loop survives. A database that is briefly unreachable must not take the
                // dispatcher down with it for the rest of the process's life.
                logger.Error(exception, "An outbox pass failed. Backing off.");
                interval = TimeSpan.FromSeconds(_options.BackoffSecondsAfterFailure);
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.Information("The outbox processor has stopped.");
    }
}
