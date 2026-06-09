using System.Diagnostics;
using Finbridge.Api.Services;
using Finbridge.Data.Outbox;
using Finbridge.Domain.Users.Events;
using Microsoft.Extensions.Options;

namespace Finbridge.Api.Outbox;

public sealed class BalanceUpdatedOutboxPublisher : IOutboxPublisher
{
    public string MessageType => typeof(BalanceUpdatedDomainEvent).FullName!;

    private readonly IKafkaProducer _producer;
    private readonly KafkaSettings _settings;

    public BalanceUpdatedOutboxPublisher(
        IKafkaProducer producer,
        IOptions<KafkaSettings> settings)
    {
        _producer = producer;
        _settings = settings.Value;
    }

    public async ValueTask PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        using var activity = OutboxTelemetry.ActivitySource.StartActivity("outbox.publish", ActivityKind.Producer);
        if (activity is not null)
        {
            activity.SetTag("messaging.system", "kafka");
            activity.SetTag("messaging.destination", _settings.Topic);
            activity.SetTag("outbox.message.id", message.Id);
            activity.SetTag("outbox.message.type", message.MessageType);
            activity.SetTag("outbox.retry.count", message.RetryCount);
        }

        try
        {
            await _producer.ProduceAsync(_settings.Topic, message.Payload, cancellationToken);
        }
        catch (Exception ex)
        {
            if (activity is not null)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            }
            throw;
        }
    }
}
