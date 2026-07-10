using System.Collections.Concurrent;

namespace AutoBus;

/// <summary>
/// Tracks request handler registrations so <see cref="IRequestClient{TRequest,TResponse}"/> can
/// enforce its exactly-one-handler contract.
/// </summary>
internal sealed class RequestHandlerRegistry
{
    private readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), int> _handlerCounts = new();

    internal void RegisterHandler(Type requestType, Type responseType)
        => _handlerCounts.AddOrUpdate((requestType, responseType), 1, static (_, count) => count + 1);

    /// <summary>
    /// Number of handlers registered for the <paramref name="requestType"/>/<paramref name="responseType"/> pair.
    /// </summary>
    public int GetHandlerCount(Type requestType, Type responseType)
        => _handlerCounts.TryGetValue((requestType, responseType), out var count) ? count : 0;
}
