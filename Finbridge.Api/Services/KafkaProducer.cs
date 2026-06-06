using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Finbridge.Api.Services;

public sealed class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly string _defaultTopic;

    public KafkaProducer(IOptions<KafkaSettings> settings)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = settings.Value.BootstrapServers,
            LingerMs = 5,
            BatchSize = 64 * 1024
        };
        _producer = new ProducerBuilder<Null, string>(config).Build();
        _defaultTopic = settings.Value.Topic;
    }

    public async Task ProduceAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        var targetTopic = string.IsNullOrWhiteSpace(topic) ? _defaultTopic : topic;
        await _producer.ProduceAsync(
            targetTopic,
            new Message<Null, string> { Value = payload },
            cancellationToken);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}
