namespace Finbridge.Domain.Common;

/// <summary>
/// Диспетчер доменных событий. Инфраструктурная деталь — реализация
/// живёт в Application и публикует события во внешние каналы (Kafka, логи, ...).
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
