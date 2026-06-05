using System.Collections.ObjectModel;
using Finbridge.Domain.Common;
using Finbridge.Domain.Users.Events;
using Finbridge.Domain.Users.Exceptions;
using Finbridge.Domain.Users.ValueObjects;

namespace Finbridge.Domain.Users;

public sealed class User : AggregateRoot<int>
{
    public FullName FullName { get; private set; } = null!;
    public DateTime DateOfBirth { get; private set; }
    public string PlaceOfBirth { get; private set; } = string.Empty;
    public Money Balance { get; private set; } = Money.Zero;
    public uint Version { get; private set; }

    private readonly List<BalanceHistory> _history = new();
    public IReadOnlyCollection<BalanceHistory> History =>
        new ReadOnlyCollection<BalanceHistory>(_history);

    private User() { }

    public static User NewUser(FullName fullName, DateTime dateOfBirth, string placeOfBirth)
    {
        if (string.IsNullOrWhiteSpace(placeOfBirth))
        {
            throw new ArgumentException("Место рождения не может быть пустым.", nameof(placeOfBirth));
        }

        if (dateOfBirth > DateTime.UtcNow)
        {
            throw new ArgumentException("Дата рождения не может быть в будущем.", nameof(dateOfBirth));
        }

        return new User
        {
            FullName = fullName,
            DateOfBirth = dateOfBirth,
            PlaceOfBirth = placeOfBirth.Trim(),
            Balance = Money.Zero,
            Version = 0
        };
    }

    public void UpdateBalance(Money delta, Money maxBalance, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(delta);
        ArgumentNullException.ThrowIfNull(maxBalance);

        var newBalance = Balance + delta;
        if (newBalance.IsNegative)
        {
            throw new NegativeBalanceException(newBalance);
        }

        if (newBalance > maxBalance)
        {
            throw new BalanceLimitExceededException(newBalance, maxBalance);
        }

        var oldBalance = Balance;
        Balance = newBalance;
        Version++;

        _history.Add(BalanceHistory.NewEntry(delta, newBalance, now));
        AddDomainEvent(new BalanceUpdatedDomainEvent(
            userId: Id,
            fullName: FullName.Value,
            oldBalance: oldBalance.Amount,
            newBalance: newBalance.Amount,
            delta: delta.Amount,
            occurredOn: now));
    }
}
