# AutoBus.Analyzers migration guide

Use this guide when introducing `Swevo.AutoBus.Analyzers` into an existing codebase.

## 1) Install package

```bash
dotnet add package Swevo.AutoBus.Analyzers
```

## 2) Fix warnings by rule

### ABUS001 (missing await)

Before:

```csharp
bus.PublishAsync(message);
```

After:

```csharp
await bus.PublishAsync(message);
```

### ABUS002 (broad catch)

Before:

```csharp
catch (Exception)
{
}
```

After:

```csharp
catch (InvalidOperationException)
{
}
```

### ABUS003 (excessive retries)

Before:

```csharp
cfg.UseRetry(99);
```

After:

```csharp
cfg.UseRetry(10);
```

### ABUS004 (missing cancellation token forwarding)

Before:

```csharp
await bus.SendAsync(command);
```

After:

```csharp
await bus.SendAsync(command, cancellationToken);
```

### ABUS005 (cooldown without tracing)

Before:

```csharp
cfg.UseConsumerFailureCooldown(TimeSpan.FromSeconds(10));
```

After:

```csharp
cfg.UseConsumerFailureCooldown(TimeSpan.FromSeconds(10));
cfg.EnableDeliveryTracing();
```

## 3) Suppress intentional exceptions

If a warning is intentional, prefer narrow suppression:

```csharp
#pragma warning disable ABUS003
cfg.UseRetry(25); // deliberate in this service
#pragma warning restore ABUS003
```

or in `.editorconfig` for scoped paths:

```ini
[src/Legacy/**.cs]
dotnet_diagnostic.ABUS003.severity = none
```
