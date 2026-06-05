using System.Collections.ObjectModel;

namespace Finbridge.Domain.Common;

/// <summary>
/// База для агрегатного корня. Хранит очередь доменных событий,
/// которые были подняты инвариантами и бизнес-методами агрегата.
/// </summary>
public abstract class AggregateRoot<TId>
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public TId Id { get; protected set; } = default!;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => new ReadOnlyCollection<IDomainEvent>(_domainEvents);

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
