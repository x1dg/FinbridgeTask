using Finbridge.Domain.Common;
using Finbridge.Domain.Users.ValueObjects;

namespace Finbridge.Domain.Users.Exceptions;

public sealed class BalanceLimitExceededException : DomainException
{
    public Money AttemptedBalance { get; }
    public Money MaxBalance { get; }

    public BalanceLimitExceededException(Money attemptedBalance, Money maxBalance)
        : base($"Balance {attemptedBalance} would exceed the maximum allowed value of {maxBalance}.")
    {
        AttemptedBalance = attemptedBalance;
        MaxBalance = maxBalance;
    }
}
