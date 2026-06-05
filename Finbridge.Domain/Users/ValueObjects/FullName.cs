using Finbridge.Domain.Common;

namespace Finbridge.Domain.Users.ValueObjects;

public sealed class FullName : ValueObject
{
    public string Value { get; }

    public const int MaxLength = 200;

    private FullName(string value)
    {
        Value = value;
    }

    public static FullName Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Полное имя не может быть пустым.", nameof(value));
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new ArgumentException($"Длина полного имени не может превышать {MaxLength} символов.", nameof(value));
        }

        return new FullName(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(FullName fullName) => fullName.Value;
}
