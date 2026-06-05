using Finbridge.Domain.Common;
using Finbridge.Domain.Users.ValueObjects;

namespace Finbridge.Domain.Users;

public sealed class BalanceHistory : Entity
{
    public int UserId { get; private set; }
    public Money Delta { get; private set; } = Money.Zero;
    public Money NewBalance { get; private set; } = Money.Zero;
    public DateTime ChangedAt { get; private set; }

    private BalanceHistory() { }

    internal static BalanceHistory NewEntry(Money delta, Money newBalance, DateTime changedAt)
    {
        return new BalanceHistory
        {
            Delta = delta,
            NewBalance = newBalance,
            ChangedAt = changedAt
        };
    }
}
