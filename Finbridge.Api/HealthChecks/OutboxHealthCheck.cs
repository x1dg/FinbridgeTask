using Finbridge.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Finbridge.Api.HealthChecks;

public sealed class OutboxHealthCheck : IHealthCheck
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromSeconds(60);

    private readonly FinbridgeDbContext _context;

    public OutboxHealthCheck(FinbridgeDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var oldest = await _context.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredOn)
            .Select(m => (DateTime?)m.OccurredOn)
            .FirstOrDefaultAsync(cancellationToken);

        if (oldest is null)
        {
            return HealthCheckResult.Healthy("Outbox пуст.");
        }

        var age = DateTime.UtcNow - oldest.Value;
        var data = new Dictionary<string, object> { ["oldestPendingAge"] = age };

        return age > StaleThreshold
            ? HealthCheckResult.Unhealthy(
                $"Самое старое необработанное сообщение висит {age.TotalSeconds:F0}с (порог {StaleThreshold.TotalSeconds:F0}с).",
                data: data)
            : HealthCheckResult.Healthy(
                $"Самое старое необработанное сообщение висит {age.TotalSeconds:F0}с.",
                data);
    }
}
