using Finbridge.Application.Contracts;
using Finbridge.Application.Services;
using Finbridge.Data;
using Finbridge.Data.Repositories;
using Finbridge.Domain.Common;
using Finbridge.Domain.Users.Events;
using Finbridge.Domain.Users.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Finbridge.Tests.Application;

public class BalanceServiceTests
{
    private static (BalanceService svc, FinbridgeDbContext ctx, CapturingDispatcher dispatcher) Build(
        decimal maxBalance = 1_000_000m)
    {
        var dbName = $"BalanceTestDb_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<FinbridgeDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var ctx = new FinbridgeDbContext(options);
        ctx.Database.EnsureDeleted();
        ctx.Database.EnsureCreated();

        var repo = new UserRepository(ctx);
        var dispatcher = new CapturingDispatcher();
        var svc = new BalanceService(
            repo,
            dispatcher,
            Options.Create(new BalanceSettings { MaxBalance = maxBalance }),
            NullLogger<BalanceService>.Instance);
        return (svc, ctx, dispatcher);
    }

    [Fact]
    public async Task UpdateBalanceAsync_ShouldApplyDelta_AndDispatchEvent()
    {
        var (svc, ctx, dispatcher) = Build();
        var user = await new UserService(
            new UserRepository(ctx),
            new CapturingDispatcher()).CreateAsync(
            new CreateUserRequest("Иван", new DateTime(1990, 1, 1), "Москва"));

        var result = await svc.UpdateBalanceAsync(new UpdateBalanceRequest(user.Id, 250m));

        Assert.Equal(250m, result.Balance);
        Assert.Contains(dispatcher.Events, e =>
            e is BalanceUpdatedDomainEvent b && b.UserId == user.Id && b.NewBalance == 250m);
    }

    [Fact]
    public async Task UpdateBalanceAsync_ShouldThrow_WhenUserNotFound()
    {
        var (svc, _, _) = Build();
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            svc.UpdateBalanceAsync(new UpdateBalanceRequest(999, 100m)));
    }

    [Fact]
    public async Task UpdateBalanceAsync_ShouldThrow_WhenResultWouldBeNegative()
    {
        var (svc, ctx, _) = Build();
        var user = await new UserService(new UserRepository(ctx), new CapturingDispatcher())
            .CreateAsync(new CreateUserRequest("Иван", new DateTime(1990, 1, 1), "Москва"));

        await Assert.ThrowsAsync<NegativeBalanceException>(() =>
            svc.UpdateBalanceAsync(new UpdateBalanceRequest(user.Id, -1m)));
    }

    [Fact]
    public async Task UpdateBalanceAsync_ShouldThrow_WhenExceedsMaxBalance()
    {
        var (svc, ctx, _) = Build(maxBalance: 100m);
        var user = await new UserService(new UserRepository(ctx), new CapturingDispatcher())
            .CreateAsync(new CreateUserRequest("Иван", new DateTime(1990, 1, 1), "Москва"));

        await Assert.ThrowsAsync<BalanceLimitExceededException>(() =>
            svc.UpdateBalanceAsync(new UpdateBalanceRequest(user.Id, 200m)));
    }

    [Fact]
    public async Task UpdateBalancesAsync_ShouldProcessAllItems()
    {
        var (svc, ctx, _) = Build();
        var userService = new UserService(new UserRepository(ctx), new CapturingDispatcher());
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

    private sealed class CapturingDispatcher : IDomainEventDispatcher
    {
        public List<IDomainEvent> Events { get; } = new();

        public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(domainEvent);
            return Task.CompletedTask;
        }
    }
}
