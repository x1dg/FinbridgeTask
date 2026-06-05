using System.Net.Sockets;
using System.Threading.RateLimiting;
using Confluent.Kafka;
using Finbridge.Api.Middleware;
using Finbridge.Api.Outbox;
using Finbridge.Api.Resilience;
using Finbridge.Api.Services;
using Finbridge.Application;
using Finbridge.Application.Services;
using Finbridge.Data;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Registry;
using Polly.Retry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<BalanceSettings>(builder.Configuration.GetSection("BalanceSettings"));
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("KafkaSettings"));
builder.Services.Configure<KafkaResilienceOptions>(builder.Configuration.GetSection("KafkaResilience"));

builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("DefaultConnection")!);

builder.Services.AddResiliencePipeline(ResiliencePipelines.KafkaProducer, (pipelineBuilder, context) =>
{
    var opts = context.ServiceProvider
        .GetRequiredService<IOptions<KafkaResilienceOptions>>().Value;
    var logger = context.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Finbridge.Resilience.Kafka");

    pipelineBuilder
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = opts.Retry.MaxAttempts,
            Delay = TimeSpan.FromMilliseconds(opts.Retry.BaseDelayMs),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = opts.CircuitBreaker.FailureRatio,
            MinimumThroughput = opts.CircuitBreaker.MinimumThroughput,
            SamplingDuration = TimeSpan.FromSeconds(opts.CircuitBreaker.SamplingDurationSec),
            BreakDuration = TimeSpan.FromSeconds(opts.CircuitBreaker.BreakDurationSec),

            ShouldHandle = new PredicateBuilder()
                .Handle<ProduceException<Null, string>>()
                .Handle<KafkaException>()
                .Handle<TimeoutException>()
                .Handle<HttpRequestException>()
                .Handle<SocketException>()
                .Handle<IOException>(),

            OnOpened = args =>
            {
                logger.LogError(
                    args.Outcome.Exception,
                    "Circuit breaker ОТКРЫТ на {BreakDurationSeconds}с. Последняя ошибка: {ExceptionType}.",
                    args.BreakDuration.TotalSeconds,
                    args.Outcome.Exception?.GetType().Name ?? "n/a");
                return ValueTask.CompletedTask;
            },
            OnClosed = args =>
            {
                logger.LogInformation("Circuit breaker ЗАКРЫТ — продюсер снова работает.");
                return ValueTask.CompletedTask;
            },
            OnHalfOpened = args =>
            {
                logger.LogWarning("Circuit breaker ПОЛУОТКРЫТ — пробуем продюсер следующим вызовом.");
                return ValueTask.CompletedTask;
            },
        })
        .AddRateLimiter(new RateLimiterStrategyOptions
        {
            RateLimiter = args =>
            {
                var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = opts.RateLimiter.PermitLimit,
                    Window = TimeSpan.FromSeconds(opts.RateLimiter.WindowSec),
                    SegmentsPerWindow = opts.RateLimiter.SegmentsPerWindow,
                    QueueLimit = opts.RateLimiter.QueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                });
                return limiter.AcquireAsync(1, args.Context.CancellationToken);
            }
        });
});

builder.Services.AddScoped<KafkaProducer>();
builder.Services.AddScoped<IKafkaProducer>(sp =>
    new ResilientKafkaProducer(
        sp.GetRequiredService<KafkaProducer>(),
        sp.GetRequiredService<ResiliencePipelineProvider<string>>()));

builder.Services.AddScoped<IOutboxPublisher, BalanceUpdatedOutboxPublisher>();
builder.Services.AddHostedService<OutboxRelayService>();

builder.Services.AddApplication();

builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromSeconds(10);
        options.QueueLimit = 5;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    }));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinbridgeDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandling();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();