using Finbridge.Domain.Common;

namespace Finbridge.Domain.Users.ValueObjects;

/// <summary>
/// Деньги. Положительная десятичная дробь с фиксированной точностью.
/// Инварианты: amount >= 0; не NaN/Infinity; точность — 2 знака после запятой.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }

    public static Money Zero { get; } = new(0m);

    private Money(decimal amount)
    {
        Amount = Math.Round(amount, 2, MidpointRounding.ToEven);
    }

    public static Money Of(decimal amount)
    {
        if (amount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Money amount cannot be negative.");
        }

        return new Money(amount);
    }

    /// <summary>
    /// Дельта, которая может быть и положительной, и отрицательной (например, при изменении баланса).
    /// Используется ТОЛЬКО для арифметики, не для хранения.
    /// </summary>
    public static Money DeltaOf(decimal amount) => new(Math.Round(amount, 2, MidpointRounding.ToEven));

    public bool IsZero => Amount == 0m;
    public bool IsPositive => Amount > 0m;
    public bool IsNegative => Amount < 0m;

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);
    public static Money operator -(Money left, Money right)
    {
        var result = left.Amount - right.Amount;
        if (result < 0m)
        {
            throw new InvalidOperationException("Money subtraction would produce a negative value.");
        }
        return new Money(result);
    }

    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;
    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;
    public static bool operator >=(Money left, Money right) => left.Amount >= right.Amount;
    public static bool operator <=(Money left, Money right) => left.Amount <= right.Amount;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
    }

    public override string ToString() => Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
}
