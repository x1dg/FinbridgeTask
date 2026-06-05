using Finbridge.Domain.Users;
using Finbridge.Domain.Users.Exceptions;
using Finbridge.Domain.Users.ValueObjects;

namespace Finbridge.Tests.Domain;

public class UserAggregateTests
{
    private static readonly DateTime Dob = new(1990, 1, 1);
    private const string PlaceOfBirth = "Москва";

    [Fact]
    public void Create_ShouldStartWithZeroBalance()
    {
        var user = User.NewUser(FullName.Of("Иван Иванов"), Dob, PlaceOfBirth);

        Assert.Equal(0m, user.Balance.Amount);
        Assert.Equal((uint)0, user.Version);
        Assert.Empty(user.History);
    }

    [Fact]
    public void Create_ShouldThrow_OnEmptyPlaceOfBirth()
    {
        Assert.Throws<ArgumentException>(() =>
            User.NewUser(FullName.Of("Иван Иванов"), Dob, "  "));
    }

    [Fact]
    public void Create_ShouldThrow_OnFutureDateOfBirth()
    {
        Assert.Throws<ArgumentException>(() =>
            User.NewUser(FullName.Of("Иван Иванов"), DateTime.UtcNow.AddDays(1), PlaceOfBirth));
    }

    [Fact]
    public void UpdateBalance_ShouldApplyPositiveDelta_AndRecordHistory()
    {
        var user = User.NewUser(FullName.Of("Иван Иванов"), Dob, PlaceOfBirth);
        var now = DateTime.UtcNow;

        user.UpdateBalance(Money.DeltaOf(150m), Money.Of(1_000_000m), now);

        Assert.Equal(150m, user.Balance.Amount);
        Assert.Equal((uint)1, user.Version);
        Assert.Single(user.History);
        Assert.Equal(150m, user.History.First().Delta.Amount);
        Assert.Equal(150m, user.History.First().NewBalance.Amount);
        Assert.Equal(now, user.History.First().ChangedAt);
        Assert.Single(user.DomainEvents);
    }

    [Fact]
    public void UpdateBalance_ShouldThrow_WhenResultingBalanceIsNegative()
    {
        var user = User.NewUser(FullName.Of("Иван Иванов"), Dob, PlaceOfBirth);

        Assert.Throws<NegativeBalanceException>(() =>
            user.UpdateBalance(Money.DeltaOf(-10m), Money.Of(1_000_000m), DateTime.UtcNow));
    }

    [Fact]
    public void UpdateBalance_ShouldThrow_WhenExceedingMaxBalance()
    {
        var user = User.NewUser(FullName.Of("Иван Иванов"), Dob, PlaceOfBirth);

        Assert.Throws<BalanceLimitExceededException>(() =>
            user.UpdateBalance(Money.DeltaOf(1_500_000m), Money.Of(1_000_000m), DateTime.UtcNow));
    }

    [Fact]
    public void UpdateBalance_ShouldThrow_WhenMaxBalanceIsExceeded_AfterMultipleOps()
    {
        var user = User.NewUser(FullName.Of("Иван Иванов"), Dob, PlaceOfBirth);
        var max = Money.Of(1_000m);

        user.UpdateBalance(Money.DeltaOf(800m), max, DateTime.UtcNow);

        Assert.Throws<BalanceLimitExceededException>(() =>
            user.UpdateBalance(Money.DeltaOf(300m), max, DateTime.UtcNow));
    }

    [Fact]
    public void UpdateBalance_ShouldRaiseBalanceUpdatedDomainEvent_WithCorrectValues()
    {
        var user = User.NewUser(FullName.Of("Иван Иванов"), Dob, PlaceOfBirth);
        var now = DateTime.UtcNow;

        user.UpdateBalance(Money.DeltaOf(150m), Money.Of(1_000_000m), now);

        var evt = Assert.IsType<Finbridge.Domain.Users.Events.BalanceUpdatedDomainEvent>(
            user.DomainEvents.Single());

        Assert.Equal(user.Id, evt.UserId);
        Assert.Equal("Иван Иванов", evt.FullName);
        Assert.Equal(0m, evt.OldBalance);
        Assert.Equal(150m, evt.NewBalance);
        Assert.Equal(150m, evt.Delta);
        Assert.Equal(now, evt.OccurredOn);
    }

    [Fact]
    public void ClearDomainEvents_ShouldEmptyQueue()
    {
        var user = User.NewUser(FullName.Of("Иван Иванов"), Dob, PlaceOfBirth);
        user.UpdateBalance(Money.DeltaOf(10m), Money.Of(1_000_000m), DateTime.UtcNow);
        Assert.NotEmpty(user.DomainEvents);

        user.ClearDomainEvents();
        Assert.Empty(user.DomainEvents);
    }
}
