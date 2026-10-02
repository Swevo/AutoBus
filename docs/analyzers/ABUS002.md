# ABUS002: Avoid broad catch in consumer/handler

## Why this matters

`catch (Exception)` in AutoBus consumers/handlers can swallow failures that should flow through retry/dead-letter handling.

## Trigger

`catch (Exception)` inside a type implementing:

- `IConsumer<T>`
- `IRequestHandler<TRequest, TResponse>`

## Fix

Use the code fix: **“Replace with InvalidOperationException”** and then tune to a domain-specific exception.

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

## Suppression (when intentional)

```csharp
#pragma warning disable ABUS002
catch (Exception) { /* boundary handler */ }
#pragma warning restore ABUS002
```
