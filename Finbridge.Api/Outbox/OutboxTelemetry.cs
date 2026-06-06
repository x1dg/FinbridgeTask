using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Finbridge.Api.Outbox;

public static class OutboxTelemetry
{
    public const string SourceName = "Finbridge.Outbox";
    public const string MeterName = "Finbridge.Outbox";

    public static readonly ActivitySource ActivitySource = new(SourceName);
    public static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> PublishedMessages =
        Meter.CreateCounter<long>("outbox.published", description: "Количество успешно опубликованных outbox-сообщений.");

    public static readonly Counter<long> FailedMessages =
        Meter.CreateCounter<long>("outbox.failed", description: "Количество outbox-сообщений с ошибкой публикации.");

    public static readonly Histogram<double> PublishDurationMs =
        Meter.CreateHistogram<double>("outbox.publish.duration", unit: "ms", description: "Длительность публикации одного сообщения в Kafka.");

    public static readonly ObservableGauge<long> PendingCount =
        Meter.CreateObservableGauge("outbox.pending", () => Interlocked.Read(ref _pendingCount), description: "Текущее количество необработанных сообщений в outbox.");

    private static long _pendingCount;

    public static void SetPendingCount(long count) => Interlocked.Exchange(ref _pendingCount, count);
}
