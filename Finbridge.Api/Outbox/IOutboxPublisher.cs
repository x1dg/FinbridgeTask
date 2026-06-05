using Finbridge.Data.Outbox;

namespace Finbridge.Api.Outbox;

public interface IOutboxPublisher
{
    string MessageType { get; }
    ValueTask PublishAsync(OutboxMessage message, CancellationToken cancellationToken);
}
