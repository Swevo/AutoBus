# Swevo.AutoBus.Analyzers

Roslyn analyzers for common AutoBus reliability mistakes.

## Rules

- **ABUS001**: Await `IMessageBus`/`IMessageScheduler` async calls (`PublishAsync`, `SendAsync`, `SchedulePublishAsync`).
- **ABUS002**: Avoid broad `catch (Exception)` in `IConsumer<T>` / `IRequestHandler<TRequest, TResponse>`.
- **ABUS003**: Avoid excessively large retry counts in `UseRetry(...)` configuration.
- **ABUS004**: Forward available `CancellationToken` to AutoBus async calls.
- **ABUS005**: Pair `UseConsumerFailureCooldown(...)` with `EnableDeliveryTracing()`.

Each rule ships with a code fix.

Rule docs:

- [`ABUS001`](../../docs/analyzers/ABUS001.md)
- [`ABUS002`](../../docs/analyzers/ABUS002.md)
- [`ABUS003`](../../docs/analyzers/ABUS003.md)
- [`ABUS004`](../../docs/analyzers/ABUS004.md)
- [`ABUS005`](../../docs/analyzers/ABUS005.md)
- [`Migration guide`](../../docs/analyzers/MIGRATION.md)

## Install

```bash
dotnet add package Swevo.AutoBus.Analyzers
```
