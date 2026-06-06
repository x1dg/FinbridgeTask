using Finbridge.Api.HealthChecks;
using Finbridge.Data;
using Finbridge.Data.Interceptors;
using Finbridge.Domain.Users;
using Finbridge.Domain.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Finbridge.Tests.Api;

public class OutboxHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WithNoPendingMessages_ReturnsHealthy()
    {
        await using var ctx = BuildContext();
        var time = new FixedTimeProvider(DateTime.UtcNow);

        var check = new OutboxHealthCheck(ctx, time);
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WithRecentMessage_ReturnsHealthy()
    {
        await using var ctx = BuildContext();
        var user = User.NewUser(FullName.Of("Иван Иванов"), new DateTime(1990, 1, 1), "Москва");
        user.UpdateBalance(Money.DeltaOf(10m), Money.Of(1_000_000m), DateTime.UtcNow);
        await ctx.Users.AddAsync(user);
        await ctx.SaveChangesAsync();

        var time = new FixedTimeProvider(DateTime.UtcNow.AddSeconds(30));
        var check = new OutboxHealthCheck(ctx, time);
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WithStaleMessage_ReturnsUnhealthy()
    {
        await using var ctx = BuildContext();
        var user = User.NewUser(FullName.Of("Иван Иванов"), new DateTime(1990, 1, 1), "Москва");
        user.UpdateBalance(Money.DeltaOf(10m), Money.Of(1_000_000m), DateTime.UtcNow);
        await ctx.Users.AddAsync(user);
        await ctx.SaveChangesAsync();

        var time = new FixedTimeProvider(DateTime.UtcNow.AddSeconds(120));
        var check = new OutboxHealthCheck(ctx, time);
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.ContainsKey("oldestPendingAge"));
    }

    private static FinbridgeDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<FinbridgeDbContext>()
            .UseInMemoryDatabase($"OutboxHealthTest_{Guid.NewGuid():N}")
            .AddInterceptors(new OutboxSaveChangesInterceptor())
            .Options;
        var ctx = new FinbridgeDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTime utcNow)
        {
            _now = new DateTimeOffset(utcNow, TimeSpan.Zero);
        }

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
