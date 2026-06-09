using Finbridge.Data;
using Finbridge.Data.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Finbridge.Api.Outbox;

public sealed class OutboxRelayService : BackgroundService
{
    private const int BatchSize = 100;
    private const int MaxRetries = 10;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxRelayService> _logger;

    public OutboxRelayService(IServiceScopeFactory scopeFactory, ILogger<OutboxRelayService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox relay запущен. Интервал опроса: {IntervalSeconds}с.", PollInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var pollActivity = OutboxTelemetry.ActivitySource.StartActivity("outbox.poll");
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Необработанная ошибка в фоновом цикле outbox relay.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) { }
        }

        _logger.LogInformation("Outbox relay остановлен.");
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FinbridgeDbContext>();
        var publishers = scope.ServiceProvider
            .GetServices<IOutboxPublisher>()
            .ToDictionary(p => p.MessageType);

        var pending = await ClaimPendingMessagesAsync(context, BatchSize, MaxRetries, cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        var processed = 0;
        var failed = 0;

        foreach (var message in pending)
        {
            if (!publishers.TryGetValue(message.MessageType, out var publisher))
            {
                _logger.LogWarning(
                    "Нет издателя для сообщения outbox {MessageId} типа {MessageType}. Пропуск.",
                    message.Id, message.MessageType);
                message.IncrementRetry("Нет зарегистрированного издателя");
                OutboxTelemetry.FailedMessages.Add(1);
                failed++;
                continue;
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await publisher.PublishAsync(message, cancellationToken);
                sw.Stop();
                OutboxTelemetry.PublishDurationMs.Record(sw.Elapsed.TotalMilliseconds);
                OutboxTelemetry.PublishedMessages.Add(1);
                message.MarkProcessed();
                processed++;
            }
            catch (Exception ex)
            {
                sw.Stop();
                OutboxTelemetry.PublishDurationMs.Record(sw.Elapsed.TotalMilliseconds);
                OutboxTelemetry.FailedMessages.Add(1);
                _logger.LogError(ex,
                    "Ошибка публикации outbox-сообщения {MessageId} типа {MessageType}.",
                    message.Id, message.MessageType);
                message.IncrementRetry(ex.Message);
                failed++;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        OutboxTelemetry.SetPendingCount(await context.OutboxMessages.CountAsync(m => m.ProcessedAt == null, cancellationToken));

        if (processed > 0 || failed > 0)
        {
            _logger.LogDebug("Outbox relay: обработано {Processed}, с ошибкой {Failed}.", processed, failed);
        }
    }

    private static async Task<List<OutboxMessage>> ClaimPendingMessagesAsync(
        FinbridgeDbContext context,
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken)
    {
        if (context.Database.IsNpgsql())
        {
            return await context.OutboxMessages
                .FromSqlRaw(
                    "SELECT * FROM outbox_messages " +
                    "WHERE \"ProcessedAt\" IS NULL AND \"RetryCount\" < {0} " +
                    "ORDER BY \"OccurredOn\" " +
                    "LIMIT {1} " +
                    "FOR UPDATE SKIP LOCKED",
                    maxRetries, batchSize)
                .ToListAsync(cancellationToken);
        }

        return await context.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < maxRetries)
            .OrderBy(m => m.OccurredOn)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
