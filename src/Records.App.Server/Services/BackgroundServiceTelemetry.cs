using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Records.App.Server.Services;

internal static class BackgroundServiceTelemetry
{
    public const string ActivitySourceName = "Records.BackgroundServices";
    public const string MeterName = "Records.BackgroundServices";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> Runs = Meter.CreateCounter<long>(
        "records.background.run.count",
        "{run}",
        "Number of background processing loops executed.");

    public static readonly Counter<long> Failures = Meter.CreateCounter<long>(
        "records.background.run.failures",
        "{failure}",
        "Number of background processing loops that failed.");

    public static readonly Counter<long> RetriesScheduled = Meter.CreateCounter<long>(
        "records.background.retry.scheduled",
        "{retry}",
        "Number of retries scheduled by background services.");

    public static readonly Counter<long> ItemsProcessed = Meter.CreateCounter<long>(
        "records.background.items.processed",
        "{item}",
        "Number of items processed by background services.");

    public static readonly Histogram<double> RunDuration = Meter.CreateHistogram<double>(
        "records.background.run.duration",
        "ms",
        "Background processing loop duration in milliseconds.");

    public static void RecordRun(string service, string outcome, int processed, double durationMs)
    {
        var tags = new TagList
        {
            { "service", service },
            { "outcome", outcome }
        };

        Runs.Add(1, tags);
        RunDuration.Record(durationMs, tags);

        if (processed > 0)
        {
            ItemsProcessed.Add(processed, new TagList
            {
                { "service", service }
            });
        }

        if (string.Equals(outcome, "failure", StringComparison.OrdinalIgnoreCase))
        {
            Failures.Add(1, new TagList
            {
                { "service", service }
            });
        }
    }

    public static void RecordRetryScheduled(string service, string reason)
    {
        RetriesScheduled.Add(1, new TagList
        {
            { "service", service },
            { "reason", reason }
        });
    }
}