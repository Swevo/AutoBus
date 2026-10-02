# ABUS001: Await AutoBus async call

## Why this matters

Fire-and-forget bus/scheduler calls hide delivery failures and retry exhaustion from callers.

## Trigger

- `IMessageBus.PublishAsync(...)`
- `IMessageBus.SendAsync(...)`
- `IMessageScheduler.SchedulePublishAsync(...)`

used as standalone statements without `await`.

## Fix

Use the code fix: **“Await bus call”**.

Before:

```csharp
bus.PublishAsync(message);
```

After:

```csharp
await bus.PublishAsync(message);
```
