namespace AutoBus.RabbitMQ;

/// <summary>
/// Deterministic exchange/queue naming for a given message type. Kept as a small, pure,
/// independently-testable class since it's the part of this transport that doesn't require
/// a live broker to verify.
/// </summary>
public static class RabbitMqTopology
{
    /// <summary>
    /// Fanout exchange name for <paramref name="messageType"/>. One exchange per message type;
    /// every service interested in the message binds its own durable queue to it.
    /// </summary>
    public static string ExchangeNameFor(Type messageType) => SanitizeName(messageType.FullName ?? messageType.Name);

    /// <summary>
    /// Durable queue name for <paramref name="messageType"/> consumed by this service instance,
    /// scoped by <paramref name="queuePrefix"/> so multiple services on the same broker don't collide.
    /// </summary>
    public static string QueueNameFor(Type messageType, string queuePrefix)
        => $"{SanitizeName(queuePrefix)}.{SanitizeName(messageType.FullName ?? messageType.Name)}";

    private static string SanitizeName(string name) => name.Replace('+', '.');
}
