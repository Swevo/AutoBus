# ABUS003: Retry count too high

## Why this matters

Very high retry counts can create long outage tails, duplicate side effects, and queue starvation.

## Trigger

`UseRetry(retryCount, ...)` where `retryCount` is a constant greater than 10.

## Fix

Use the code fix: **“Use bounded retry count (10)”** and then tune based on real SLO/error budgets.

Before:

```csharp
cfg.UseRetry(99);
```

After:

```csharp
cfg.UseRetry(10);
```
