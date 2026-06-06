using Finbridge.Api.Outbox;
using Finbridge.Data;
using Finbridge.Data.Interceptors;
using Finbridge.Data.Outbox;
using Finbridge.Domain.Users;
using Finbridge.Domain.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Finbridge.Tests.Data;

public class OutboxRelayServiceTests
{
    private static (OutboxRelayService service, FakeOutboxPublisher publisher, FinbridgeDbContext ctx) Build()
    {
        var dbName = $"OutboxRelayTest_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<FinbridgeDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(new OutboxSaveChangesInterceptor())
            .Options;
        var ctx = new FinbridgeDbContext(options);
        ctx.Database.EnsureCreated();

        var publisher = new FakeOutboxPublisher();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(ctx);
        services.AddSingleton<IOutboxPublisher>(publisher);
        var sp = services.BuildServiceProvider();

        var service = new OutboxRelayService(
            sp.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<OutboxRelayService>.Instance);

        return (service, publisher, ctx);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPublishPendingMessage_AndMarkProcessed()
    {
        var (service, publisher, ctx) = Build();

        var user = User.NewUser(FullName.Of("Иван Иванов"), new DateTime(1990, 1, 1), "Москва");
        user.UpdateBalance(Money.DeltaOf(100m), Money.Of(1_000_000m), DateTime.UtcNow);
        await ctx.Users.AddAsync(user);
        await ctx.SaveChangesAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(500, cts.Token);
        }
        catch (OperationCanceledException) { }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }

        Assert.Equal(1, publisher.PublishCount);
        Assert.Single(publisher.LastMessageIds);
        var processed = await ctx.OutboxMessages.AsNoTracking().SingleAsync();
        Assert.NotNull(processed.ProcessedAt);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIncrementRetry_OnPublishFailure()
    {
        var (service, publisher, ctx) = Build();
        publisher.ShouldThrow = true;

        var user = User.NewUser(FullName.Of("Иван Иванов"), new DateTime(1990, 1, 1), "Москва");
        user.UpdateBalance(Money.DeltaOf(100m), Money.Of(1_000_000m), DateTime.UtcNow);
        await ctx.Users.AddAsync(user);
        await ctx.SaveChangesAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(500, cts.Token);
        }
        catch (OperationCanceledException) { }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }

        Assert.Equal(1, publisher.PublishCount);
        var msg = await ctx.OutboxMessages.AsNoTracking().SingleAsync();
        Assert.Null(msg.ProcessedAt);
        Assert.Equal(1, msg.RetryCount);
        Assert.NotNull(msg.LastError);
    }

    private sealed class FakeOutboxPublisher : IOutboxPublisher
    {
        public string MessageType => typeof(Finbridge.Domain.Users.Events.BalanceUpdatedDomainEvent).FullName!;
        public bool ShouldThrow { get; set; }
        public int PublishCount { get; private set; }
        public List<Guid> LastMessageIds { get; } = new();

        public ValueTask PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            PublishCount++;
            LastMessageIds.Add(message.Id);
            if (ShouldThrow)
            {
                throw new InvalidOperationException("test failure");
            }
            return ValueTask.CompletedTask;
        }
    }
}
