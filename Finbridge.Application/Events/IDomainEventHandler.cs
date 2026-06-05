using Finbridge.Domain.Common;

namespace Finbridge.Application.Events;

/// <summary>
/// In-process обработчик доменного события. Реализуется в Application или
/// инфраструктурных слоях для побочных эффектов (Kafka, лог, метрики, ...).
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
