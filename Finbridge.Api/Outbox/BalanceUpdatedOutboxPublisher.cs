using Finbridge.Api.Resilience;
using Finbridge.Api.Services;
using Finbridge.Data.Outbox;
using Finbridge.Domain.Users.Events;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;

namespace Finbridge.Api.Outbox;

public sealed class BalanceUpdatedOutboxPublisher : IOutboxPublisher
{
    private readonly IKafkaProducer _producer;
    private readonly KafkaSettings _settings;
    private readonly ResiliencePipeline _pipeline;

    public string MessageType => typeof(BalanceUpdatedDomainEvent).FullName!;

    public BalanceUpdatedOutboxPublisher(
        IKafkaProducer producer,
        IOptions<KafkaSettings> settings,
        ResiliencePipelineProvider<string> pipelineProvider)
    {
        _producer = producer;
        _settings = settings.Value;
        _pipeline = pipelineProvider.GetPipeline(ResiliencePipelines.KafkaProducer);
    }

    public ValueTask PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        return _pipeline.ExecuteAsync(
            async ct => await _producer.ProduceAsync(_settings.Topic, message.Payload, ct),
            cancellationToken);
    }
}
