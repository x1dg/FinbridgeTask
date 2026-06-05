using Finbridge.Domain.Common;
using Finbridge.Domain.Users.ValueObjects;

namespace Finbridge.Domain.Users.Exceptions;

public sealed class BalanceLimitExceededException : DomainException
{
    public Money AttemptedBalance { get; }
    public Money MaxBalance { get; }

    public BalanceLimitExceededException(Money attemptedBalance, Money maxBalance)
        : base($"Баланс {attemptedBalance} превысил бы максимально допустимое значение {maxBalance}.")
    {
        AttemptedBalance = attemptedBalance;
        MaxBalance = maxBalance;
    }
}
