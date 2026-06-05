using Finbridge.Domain.Common;
using Finbridge.Domain.Users.ValueObjects;

namespace Finbridge.Domain.Users.Exceptions;

public sealed class NegativeBalanceException : DomainException
{
    public Money AttemptedBalance { get; }

    public NegativeBalanceException(Money attemptedBalance)
        : base($"Operation would result in a negative balance ({attemptedBalance}).")
    {
        AttemptedBalance = attemptedBalance;
    }
}
