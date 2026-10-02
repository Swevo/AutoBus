using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AutoBus;

internal static class AutoBusTelemetry
{
    public static readonly ActivitySource ActivitySource = new("Swevo.AutoBus");
    private static readonly Meter Meter = new("Swevo.AutoBus");

    public static readonly Counter<long> DeliveriesStarted = Meter.CreateCounter<long>("autobus.deliveries.started");
    public static readonly Counter<long> DeliveriesSucceeded = Meter.CreateCounter<long>("autobus.deliveries.succeeded");
    public static readonly Counter<long> DeliveriesFailed = Meter.CreateCounter<long>("autobus.deliveries.failed");
    public static readonly Histogram<double> DeliveryDurationMs = Meter.CreateHistogram<double>("autobus.deliveries.duration.ms");
}
