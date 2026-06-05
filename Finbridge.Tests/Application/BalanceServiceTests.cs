using Finbridge.Application.Contracts;
using Finbridge.Application.Services;
using Finbridge.Data;
using Finbridge.Data.Interceptors;
using Finbridge.Data.Outbox;
using Finbridge.Data.Repositories;
using Finbridge.Domain.Users.Events;
using Finbridge.Domain.Users.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Finbridge.Tests.Application;

public class BalanceServiceTests
{
    private static (BalanceService svc, FinbridgeDbContext ctx) Build(decimal maxBalance = 1_000_000m)
    {
        var dbName = $"BalanceTestDb_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<FinbridgeDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(new OutboxSaveChangesInterceptor())
            .Options;
        var ctx = new FinbridgeDbContext(options);
        ctx.Database.EnsureDeleted();
        ctx.Database.EnsureCreated();

        var repo = new UserRepository(ctx);
        var svc = new BalanceService(
            repo,
            Options.Create(new BalanceSettings { MaxBalance = maxBalance }));
        return (svc, ctx);
    }

    [Fact]
    public async Task UpdateBalanceAsync_ShouldApplyDelta_AndEnqueueOutboxMessage()
    {
        var (svc, ctx) = Build();
        var user = await new UserService(new UserRepository(ctx)).CreateAsync(
            new CreateUserRequest("Иван", new DateTime(1990, 1, 1), "Москва"));

        var result = await svc.UpdateBalanceAsync(new UpdateBalanceRequest(user.Id, 250m));

        Assert.Equal(250m, result.Balance);

        var pending = await ctx.OutboxMessages
            .Where(m => m.MessageType == typeof(BalanceUpdatedDomainEvent).FullName)
            .ToListAsync();
        Assert.Single(pending);
        Assert.Null(pending[0].ProcessedAt);
        Assert.Contains(user.Id.ToString(), pending[0].Payload);
    }

    [Fact]
    public async Task UpdateBalanceAsync_ShouldThrow_WhenUserNotFound()
    {
        var (svc, _) = Build();
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            svc.UpdateBalanceAsync(new UpdateBalanceRequest(999, 100m)));
    }

    [Fact]
    public async Task UpdateBalanceAsync_ShouldThrow_WhenResultWouldBeNegative()
    {
        var (svc, ctx) = Build();
        var user = await new UserService(new UserRepository(ctx)).CreateAsync(
            new CreateUserRequest("Иван", new DateTime(1990, 1, 1), "Москва"));

        await Assert.ThrowsAsync<NegativeBalanceException>(() =>
            svc.UpdateBalanceAsync(new UpdateBalanceRequest(user.Id, -1m)));
    }

    [Fact]
    public async Task UpdateBalanceAsync_ShouldThrow_WhenExceedsMaxBalance()
    {
        var (svc, ctx) = Build(maxBalance: 100m);
        var user = await new UserService(new UserRepository(ctx)).CreateAsync(
            new CreateUserRequest("Иван", new DateTime(1990, 1, 1), "Москва"));

        await Assert.ThrowsAsync<BalanceLimitExceededException>(() =>
            svc.UpdateBalanceAsync(new UpdateBalanceRequest(user.Id, 200m)));
    }

    [Fact]
    public async Task UpdateBalancesAsync_ShouldProcessAllItems()
    {
        var (svc, ctx) = Build();
        var userService = new UserService(new UserRepository(ctx));
        var u1 = await userService.CreateAsync(new CreateUserRequest("А", new DateTime(1990, 1, 1), "X"));
        var u2 = await userService.CreateAsync(new CreateUserRequest("Б", new DateTime(1990, 1, 1), "Y"));

        await svc.UpdateBalancesAsync(new BatchUpdateBalancesRequest(new[]
        {
            new UpdateBalanceRequest(u1.Id, 10m),
            new UpdateBalanceRequest(u2.Id, 20m),
        }));

        var h1 = await svc.GetBalanceHistoryAsync(u1.Id);
        var h2 = await svc.GetBalanceHistoryAsync(u2.Id);
        Assert.Single(h1);
        Assert.Single(h2);
    }
}
