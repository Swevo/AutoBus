# ABUS004: Forward cancellation token

## Why this matters

If your method already receives a `CancellationToken`, AutoBus calls should forward it so upstream cancellations can stop message operations early.

## Trigger

- `PublishAsync(...)`
- `SendAsync(...)`
- `SchedulePublishAsync(...)`

called without a cancellation token argument when the containing method has a `CancellationToken` parameter.

## Before

```csharp
public async Task HandleAsync(IMessageBus bus, CancellationToken cancellationToken)
{
    await bus.PublishAsync(new OrderCreated(42));
}
```

## After

```csharp
public async Task HandleAsync(IMessageBus bus, CancellationToken cancellationToken)
{
    await bus.PublishAsync(new OrderCreated(42), cancellationToken);
}
```

## Suppression (when intentional)

```csharp
#pragma warning disable ABUS004
await bus.PublishAsync(new OrderCreated(42));
#pragma warning restore ABUS004
```
