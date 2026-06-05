using Finbridge.Domain.Common;
using Finbridge.Domain.Users.ValueObjects;

namespace Finbridge.Domain.Users.Exceptions;

public sealed class NegativeBalanceException : DomainException
{
    public Money AttemptedBalance { get; }

    public NegativeBalanceException(Money attemptedBalance)
        : base($"Операция привела бы к отрицательному балансу ({attemptedBalance}).")
    {
        AttemptedBalance = attemptedBalance;
    }
}
