# ABUS005: Cooldown requires tracing

## Why this matters

`UseConsumerFailureCooldown(...)` intentionally drops/defers deliveries while a consumer is blocked. Without tracing, those skips are hard to diagnose.

## Trigger

`UseConsumerFailureCooldown(...)` is configured in `AddAutoBus(...)` but `EnableDeliveryTracing()` is missing in the same configuration lambda.

## Before

```csharp
services.AddAutoBus(cfg =>
{
    cfg.AddConsumer<OrderConsumer>();
    cfg.UseConsumerFailureCooldown(TimeSpan.FromSeconds(10));
});
```

## After

```csharp
services.AddAutoBus(cfg =>
{
    cfg.AddConsumer<OrderConsumer>();
    cfg.UseConsumerFailureCooldown(TimeSpan.FromSeconds(10));
    cfg.EnableDeliveryTracing();
});
```

## Suppression (when intentional)

```csharp
#pragma warning disable ABUS005
cfg.UseConsumerFailureCooldown(TimeSpan.FromSeconds(10));
#pragma warning restore ABUS005
```
