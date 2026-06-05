using Finbridge.Domain.Users.ValueObjects;

namespace Finbridge.Tests.Domain;

public class MoneyTests
{
    [Fact]
    public void Of_ShouldThrow_OnNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.Of(-1m));
    }

    [Fact]
    public void Of_ShouldRound_ToTwoDecimalPlaces()
    {
        var money = Money.Of(1.235m);
        Assert.Equal(1.24m, money.Amount);
    }

    [Fact]
    public void Addition_ShouldAccumulate()
    {
        var a = Money.Of(10m);
        var b = Money.Of(5.50m);
        Assert.Equal(15.50m, (a + b).Amount);
    }

    [Fact]
    public void Subtraction_ShouldThrow_WhenResultIsNegative()
    {
        var a = Money.Of(5m);
        var b = Money.Of(10m);
        Assert.Throws<InvalidOperationException>(() => a - b);
    }

    [Fact]
    public void Equality_ShouldBeValueBased()
    {
        var a = Money.Of(100m);
        var b = Money.Of(100m);
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(10, 0, true)]
    [InlineData(0, 10, false)]
    public void IsPositive_ShouldBeCorrect(decimal a, decimal b, bool expected)
    {
        Assert.Equal(expected, (Money.Of(a) > Money.Of(b)));
    }
}
