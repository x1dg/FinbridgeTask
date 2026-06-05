namespace Finbridge.Domain.Common;

/// <summary>
/// База для сущности внутри домена. У корня агрегата — AggregateRoot,
/// у дочерних сущностей (например, BalanceHistory) — Entity.
/// </summary>
public abstract class Entity
{
    public int Id { get; protected set; }
}
