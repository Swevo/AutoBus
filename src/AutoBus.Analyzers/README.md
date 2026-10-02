# Swevo.AutoBus.Analyzers

Roslyn analyzers for common AutoBus reliability mistakes.

## Rules

- **ABUS001**: Await `IMessageBus`/`IMessageScheduler` async calls (`PublishAsync`, `SendAsync`, `SchedulePublishAsync`).
- **ABUS002**: Avoid broad `catch (Exception)` in `IConsumer<T>` / `IRequestHandler<TRequest, TResponse>`.
- **ABUS003**: Avoid excessively large retry counts in `UseRetry(...)` configuration.

## Install

```bash
dotnet add package Swevo.AutoBus.Analyzers
```
