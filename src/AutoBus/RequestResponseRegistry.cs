using System.Collections.Concurrent;

namespace AutoBus;

internal sealed class RequestResponseRegistry
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<object?>> _pendingResponses = new();

    public TaskCompletionSource<object?> Register(Guid requestId)
    {
        var completionSource = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pendingResponses.TryAdd(requestId, completionSource))
        {
            throw new InvalidOperationException($"A pending request with ID '{requestId}' is already registered.");
        }

        return completionSource;
    }

    public bool TrySetResponse(Guid requestId, object? response)
        => _pendingResponses.TryGetValue(requestId, out var completionSource)
            && completionSource.TrySetResult(response);

    public void Remove(Guid requestId)
        => _pendingResponses.TryRemove(requestId, out _);
}
