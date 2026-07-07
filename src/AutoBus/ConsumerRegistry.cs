using System.Collections.Concurrent;
using System.Linq;

namespace AutoBus;

/// <summary>
/// Tracks how many <see cref="IConsumer{TMessage}"/> implementations have been registered
/// for each message type, so <see cref="IMessageBus.SendAsync{TMessage}"/> can enforce its
/// point-to-point (exactly-one-consumer) contract.
/// </summary>
public sealed class ConsumerRegistry
{
    private readonly ConcurrentDictionary<Type, int> _consumerCounts = new();

    internal void RegisterConsumer(Type messageType)
        => _consumerCounts.AddOrUpdate(messageType, 1, static (_, count) => count + 1);

    /// <summary>Number of consumers registered for <paramref name="messageType"/>.</summary>
    public int GetConsumerCount(Type messageType)
        => _consumerCounts.TryGetValue(messageType, out var count) ? count : 0;

    /// <summary>All message types that currently have at least one registered consumer.</summary>
    public IReadOnlyCollection<Type> RegisteredMessageTypes => _consumerCounts.Keys.ToList();
}
