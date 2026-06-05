using Finbridge.Domain.Common;

namespace Finbridge.Domain.Users.Events;

/// <summary>
/// Доменное событие: баланс пользователя успешно изменён.
/// Поднимается агрегатом User, ловится прикладным слоем и публикуется
/// во внешние шины (Kafka).
/// </summary>
public sealed class BalanceUpdatedDomainEvent : IDomainEvent
{
    public int UserId { get; }
    public string FullName { get; }
    public decimal OldBalance { get; }
    public decimal NewBalance { get; }
    public decimal Delta { get; }
    public DateTime OccurredOn { get; }

    public BalanceUpdatedDomainEvent(
        int userId,
        string fullName,
        decimal oldBalance,
        decimal newBalance,
        decimal delta,
        DateTime occurredOn)
    {
        UserId = userId;
        FullName = fullName;
        OldBalance = oldBalance;
        NewBalance = newBalance;
        Delta = delta;
        OccurredOn = occurredOn;
    }
}
