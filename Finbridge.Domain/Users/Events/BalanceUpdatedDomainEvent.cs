using Finbridge.Domain.Common;

namespace Finbridge.Domain.Users.Events;

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
