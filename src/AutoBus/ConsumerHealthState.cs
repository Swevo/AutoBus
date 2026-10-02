using System.Collections.Concurrent;

namespace AutoBus;

/// <summary>
/// Lightweight per-consumer health gate used to prevent immediate repeated failures.
/// </summary>
public sealed class ConsumerHealthState
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _blockedUntilByKey = new();

    internal bool IsBlocked(string key, DateTimeOffset now, out DateTimeOffset blockedUntil)
    {
        if (_blockedUntilByKey.TryGetValue(key, out blockedUntil) && blockedUntil > now)
        {
            return true;
        }

        blockedUntil = default;
        return false;
    }

    internal void BlockFor(string key, TimeSpan duration, DateTimeOffset now)
    {
        if (duration <= TimeSpan.Zero)
        {
            return;
        }

        _blockedUntilByKey[key] = now.Add(duration);
    }

    internal void Clear(string key) => _blockedUntilByKey.TryRemove(key, out _);
}
