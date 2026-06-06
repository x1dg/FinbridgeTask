using Finbridge.Application.Abstractions;
using Finbridge.Application.Configuration;
using Finbridge.Application.Contracts;
using Finbridge.Application.Users.Commands;
using Finbridge.Application.Users.Queries;
using Finbridge.Data;
using Finbridge.Data.Interceptors;
using Finbridge.Data.Outbox;
using Finbridge.Data.Repositories;
using Finbridge.Domain.Users.Events;
using Finbridge.Domain.Users.Exceptions;
using Finbridge.Domain.Users.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Finbridge.Tests.Application;

public class BalancesHandlerTests
{
    private static (IRequestDispatcher dispatcher, FinbridgeDbContext ctx) Build(decimal maxBalance = 1_000_000m)
    {
        var dbName = $"BalancesTestDb_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<FinbridgeDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(new OutboxSaveChangesInterceptor())
            .Options;
        var ctx = new FinbridgeDbContext(options);
        ctx.Database.EnsureDeleted();
        ctx.Database.EnsureCreated();

        var repo = new UserRepository(ctx);

        var services = new ServiceCollection();
        services.AddScoped<IUserRepository>(_ => repo);
        services.AddScoped<IRequestDispatcher, RequestDispatcher>();
        services.AddScoped<IRequestHandler<CreateUserRequest, UserResponse>, CreateUserHandler>();
        services.AddScoped<IRequestHandler<UpdateBalanceRequest, UserResponse>, UpdateBalanceHandler>();
        services.AddScoped<IRequestHandler<BatchUpdateBalancesRequest, Unit>, BatchUpdateBalancesHandler>();
        services.AddScoped<IRequestHandler<GetBalanceHistoryQuery, IReadOnlyList<BalanceHistoryResponse>>, GetBalanceHistoryHandler>();
        services.AddSingleton(Options.Create(new BalanceSettings { MaxBalance = maxBalance }));
        var sp = services.BuildServiceProvider();
        return (sp.GetRequiredService<IRequestDispatcher>(), ctx);
    }

    [Fact]
    public async Task UpdateBalance_ShouldApplyDelta_AndEnqueueOutboxMessage()
    {
        var (dispatcher, ctx) = Build();
        var user = await dispatcher.SendAsync(new CreateUserRequest(
            "Иван", new DateTime(1990, 1, 1), "Москва"));

        var result = await dispatcher.SendAsync(new UpdateBalanceRequest(user.Id, 250m));

        Assert.Equal(250m, result.Balance);

        var pending = await ctx.OutboxMessages
            .Where(m => m.MessageType == typeof(BalanceUpdatedDomainEvent).FullName)
            .ToListAsync();
        Assert.Single(pending);
        Assert.Null(pending[0].ProcessedAt);
        Assert.Contains(user.Id.ToString(), pending[0].Payload);
    }

    [Fact]
    public async Task UpdateBalance_ShouldThrow_WhenUserNotFound()
    {
        var (dispatcher, _) = Build();
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            dispatcher.SendAsync(new UpdateBalanceRequest(999, 100m)));
    }

    [Fact]
    public async Task UpdateBalance_ShouldThrow_WhenResultWouldBeNegative()
    {
        var (dispatcher, _) = Build();
        var user = await dispatcher.SendAsync(new CreateUserRequest(
            "Иван", new DateTime(1990, 1, 1), "Москва"));

        await Assert.ThrowsAsync<NegativeBalanceException>(() =>
            dispatcher.SendAsync(new UpdateBalanceRequest(user.Id, -1m)));
    }

    [Fact]
    public async Task UpdateBalance_ShouldThrow_WhenExceedsMaxBalance()
    {
        var (dispatcher, _) = Build(maxBalance: 100m);
        var user = await dispatcher.SendAsync(new CreateUserRequest(
            "Иван", new DateTime(1990, 1, 1), "Москва"));

        await Assert.ThrowsAsync<BalanceLimitExceededException>(() =>
            dispatcher.SendAsync(new UpdateBalanceRequest(user.Id, 200m)));
    }

    [Fact]
    public async Task BatchUpdateBalances_ShouldProcessAllItems()
    {
        var (dispatcher, _) = Build();
        var u1 = await dispatcher.SendAsync(new CreateUserRequest("А", new DateTime(1990, 1, 1), "X"));
        var u2 = await dispatcher.SendAsync(new CreateUserRequest("Б", new DateTime(1990, 1, 1), "Y"));

        await dispatcher.SendAsync(new BatchUpdateBalancesRequest(new[]
        {
            new UpdateBalanceRequest(u1.Id, 10m),
            new UpdateBalanceRequest(u2.Id, 20m),
        }));

        var h1 = await dispatcher.SendAsync(new GetBalanceHistoryQuery(u1.Id));
        var h2 = await dispatcher.SendAsync(new GetBalanceHistoryQuery(u2.Id));
        Assert.Single(h1);
        Assert.Single(h2);
    }
}
