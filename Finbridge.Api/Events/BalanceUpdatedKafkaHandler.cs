using System.Text.Json;
using Finbridge.Api.Services;
using Finbridge.Application.Events;
using Finbridge.Domain.Users.Events;

namespace Finbridge.Api.Events;

/// <summary>
/// In-process обработчик доменного события <see cref="BalanceUpdatedDomainEvent"/>:
/// сериализует событие в JSON и публикует в Kafka-топик.
/// </summary>
public sealed class BalanceUpdatedKafkaHandler : IDomainEventHandler<BalanceUpdatedDomainEvent>
{
    private readonly IKafkaProducer _producer;
    private readonly KafkaSettings _settings;
    private readonly ILogger<BalanceUpdatedKafkaHandler> _logger;

    public BalanceUpdatedKafkaHandler(
        IKafkaProducer producer,
        Microsoft.Extensions.Options.IOptions<KafkaSettings> settings,
        ILogger<BalanceUpdatedKafkaHandler> logger)
    {
        _producer = producer;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task HandleAsync(BalanceUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            eventType = nameof(BalanceUpdatedDomainEvent).Replace("DomainEvent", string.Empty),
            userId = domainEvent.UserId,
            fullName = domainEvent.FullName,
            oldBalance = domainEvent.OldBalance,
            newBalance = domainEvent.NewBalance,
            delta = domainEvent.Delta,
            timestamp = domainEvent.OccurredOn
        };

        var json = JsonSerializer.Serialize(payload);
        try
        {
            await _producer.ProduceAsync(_settings.Topic, json, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish BalanceUpdated event for user {UserId} to topic {Topic}.",
                domainEvent.UserId, _settings.Topic);
            throw;
        }
    }
}
