namespace Finbridge.Domain.Common;

/// <summary>
/// Маркер интерфейса доменного события. Реализуется всеми событиями,
/// которые агрегат может породить в результате бизнес-операции.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
