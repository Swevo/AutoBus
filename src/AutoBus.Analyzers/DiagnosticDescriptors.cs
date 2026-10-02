using Microsoft.CodeAnalysis;

namespace AutoBus.Analyzers;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor MissingAwaitOnBusCall = new(
        id: "ABUS001",
        title: "Await AutoBus async call",
        messageFormat: "Call to '{0}' should be awaited, returned, or explicitly handled",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "AutoBus async operations should not be fire-and-forget because delivery failures and retries become invisible to the caller.");

    public static readonly DiagnosticDescriptor BroadExceptionCatchInConsumer = new(
        id: "ABUS002",
        title: "Avoid broad exception catches in consumer/handler",
        messageFormat: "Avoid catch (Exception) in '{0}'; catch a specific exception type",
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Catching System.Exception in consumers/handlers can mask failures and interfere with retry/dead-letter behavior.");

    public static readonly DiagnosticDescriptor ExcessiveRetryCount = new(
        id: "ABUS003",
        title: "Retry count is excessively large",
        messageFormat: "Retry count '{0}' is excessively large; use a bounded value (<= 10) or justify explicitly",
        category: "Resilience",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Very large retry counts can cause prolonged outages, duplicate side-effects, and queue starvation.");

    public static readonly DiagnosticDescriptor MissingCancellationTokenForwarding = new(
        id: "ABUS004",
        title: "Forward cancellation token",
        messageFormat: "Call to '{0}' should forward the available cancellation token",
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When a method already has a CancellationToken parameter, AutoBus async operations should forward it to preserve cooperative cancellation.");

    public static readonly DiagnosticDescriptor CooldownWithoutTracing = new(
        id: "ABUS005",
        title: "Cooldown configured without tracing",
        messageFormat: "UseConsumerFailureCooldown is configured without EnableDeliveryTracing",
        category: "Observability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Failure cooldowns should be paired with delivery tracing to make blocked deliveries diagnosable.");
}
