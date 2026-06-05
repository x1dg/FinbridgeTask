namespace Finbridge.Api.Services;

public interface IKafkaProducer
{
    Task ProduceAsync(string topic, string payload, CancellationToken cancellationToken = default);
}
