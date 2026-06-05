using System.Text.Json;
using Finbridge.Domain.Common;

namespace Finbridge.Data.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string MessageType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredOn { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage FromDomainEvent(IDomainEvent @event)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = @event.GetType().FullName ?? @event.GetType().Name,
            Payload = JsonSerializer.Serialize(@event, @event.GetType()),
            OccurredOn = @event.OccurredOn
        };
    }

    public void MarkProcessed() => ProcessedAt = DateTime.UtcNow;

    public void IncrementRetry(string? error)
    {
        RetryCount++;
        LastError = Truncate(error, 2000);
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is null ? null : value.Length <= maxLength ? value : value[..maxLength];
}
