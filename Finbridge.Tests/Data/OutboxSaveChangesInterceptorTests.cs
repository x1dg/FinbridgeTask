using Finbridge.Data;
using Finbridge.Data.Interceptors;
using Finbridge.Data.Outbox;
using Finbridge.Domain.Users;
using Finbridge.Domain.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Finbridge.Tests.Data;

public class OutboxSaveChangesInterceptorTests
{
    [Fact]
    public async Task SavingChanges_ShouldConvertDomainEventsToOutboxMessages()
    {
        var options = new DbContextOptionsBuilder<FinbridgeDbContext>()
            .UseInMemoryDatabase($"OutboxTest_{Guid.NewGuid():N}")
            .AddInterceptors(new OutboxSaveChangesInterceptor())
            .Options;

        await using var ctx = new FinbridgeDbContext(options);
        await ctx.Database.EnsureCreatedAsync();

        var user = User.NewUser(FullName.Of("Иван Иванов"), new DateTime(1990, 1, 1), "Москва");
        user.UpdateBalance(Money.DeltaOf(100m), Money.Of(1_000_000m), DateTime.UtcNow);
        await ctx.Users.AddAsync(user);
        await ctx.SaveChangesAsync();

        var outbox = await ctx.OutboxMessages.AsNoTracking().ToListAsync();

        Assert.Single(outbox);
        Assert.Equal(typeof(Finbridge.Domain.Users.Events.BalanceUpdatedDomainEvent).FullName, outbox[0].MessageType);
        Assert.Contains("\"NewBalance\":100", outbox[0].Payload);
        Assert.NotEqual(Guid.Empty, outbox[0].Id);
    }

    [Fact]
    public async Task SavingChanges_ShouldClearDomainEventsAfterConvert()
    {
        var options = new DbContextOptionsBuilder<FinbridgeDbContext>()
            .UseInMemoryDatabase($"OutboxTest_{Guid.NewGuid():N}")
            .AddInterceptors(new OutboxSaveChangesInterceptor())
            .Options;

        await using var ctx = new FinbridgeDbContext(options);
        await ctx.Database.EnsureCreatedAsync();

        var user = User.NewUser(FullName.Of("Иван Иванов"), new DateTime(1990, 1, 1), "Москва");
        user.UpdateBalance(Money.DeltaOf(50m), Money.Of(1_000_000m), DateTime.UtcNow);
        await ctx.Users.AddAsync(user);
        await ctx.SaveChangesAsync();

        var changed = ctx.ChangeTracker.Entries<User>().ToList();
        Assert.Empty(changed[0].Entity.DomainEvents);
    }

    [Fact]
    public async Task SavingChanges_WithNoDomainEvents_ShouldNotCreateOutboxMessages()
    {
        var options = new DbContextOptionsBuilder<FinbridgeDbContext>()
            .UseInMemoryDatabase($"OutboxTest_{Guid.NewGuid():N}")
            .AddInterceptors(new OutboxSaveChangesInterceptor())
            .Options;

        await using var ctx = new FinbridgeDbContext(options);
        await ctx.Database.EnsureCreatedAsync();

        var user = User.NewUser(FullName.Of("Иван Иванов"), new DateTime(1990, 1, 1), "Москва");
        await ctx.Users.AddAsync(user);
        await ctx.SaveChangesAsync();

        Assert.Empty(await ctx.OutboxMessages.ToListAsync());
    }
}
