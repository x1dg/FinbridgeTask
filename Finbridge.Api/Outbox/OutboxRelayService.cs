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

        var pending = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.OccurredOn)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

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
                failed++;
                continue;
            }

            try
            {
                await publisher.PublishAsync(message, cancellationToken);
                message.MarkProcessed();
                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Ошибка публикации outbox-сообщения {MessageId} типа {MessageType}.",
                    message.Id, message.MessageType);
                message.IncrementRetry(ex.Message);
                failed++;
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        if (processed > 0 || failed > 0)
        {
            _logger.LogDebug("Outbox relay: обработано {Processed}, с ошибкой {Failed}.", processed, failed);
        }
    }
}
