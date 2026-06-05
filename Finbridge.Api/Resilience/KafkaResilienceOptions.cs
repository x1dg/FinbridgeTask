namespace Finbridge.Api.Resilience;

public sealed class KafkaResilienceOptions
{
    public RetryOptions Retry { get; set; } = new();
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
    public RateLimiterOptions RateLimiter { get; set; } = new();

    public sealed class RetryOptions
    {
        public int MaxAttempts { get; set; } = 3;
        public int BaseDelayMs { get; set; } = 200;
    }

    public sealed class CircuitBreakerOptions
    {
        public double FailureRatio { get; set; } = 0.5;
        public int MinimumThroughput { get; set; } = 5;
        public int SamplingDurationSec { get; set; } = 30;
        public int BreakDurationSec { get; set; } = 15;
    }

    public sealed class RateLimiterOptions
    {
        public int PermitLimit { get; set; } = 100;
        public int WindowSec { get; set; } = 1;
        public int SegmentsPerWindow { get; set; } = 10;
        public int QueueLimit { get; set; } = 50;
    }
}
