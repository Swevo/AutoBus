# AutoBus

[![NuGet](https://img.shields.io/nuget/v/Swevo.AutoBus.svg)](https://www.nuget.org/packages/Swevo.AutoBus/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Swevo.AutoBus.svg)](https://www.nuget.org/packages/Swevo.AutoBus/)
[![CI](https://github.com/Swevo/AutoBus/actions/workflows/build.yml/badge.svg)](https://github.com/Swevo/AutoBus/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Free, MIT-licensed message bus for .NET.** No commercial license required — ever.

## Why AutoBus?

MassTransit v9 is now a commercial product. AutoBus is a much smaller, focused alternative
for the common case: a modular monolith (or a small set of services) that wants reliable
pub/sub and point-to-point messaging with retry, without adopting a paid distributed
messaging framework. It isn't a drop-in MassTransit replacement — there's no saga state
machine engine, no routing slips, no multi-broker rider abstraction — just consumers,
publish/send, retry, and (optionally) RabbitMQ.

```csharp
public sealed class OrderCreated
{
    public int OrderId { get; init; }
}

public sealed class SendWelcomeEmail : IConsumer<OrderCreated>
{
    public Task Consume(ConsumeContext<OrderCreated> context)
    {
        // send the email...
        return Task.CompletedTask;
    }
}

services.AddAutoBus(cfg => cfg.AddConsumer<SendWelcomeEmail>());

// elsewhere:
await messageBus.PublishAsync(new OrderCreated { OrderId = 42 });
```

## Install

```bash
dotnet add package Swevo.AutoBus
```

For cross-process messaging over RabbitMQ:

```bash
dotnet add package Swevo.AutoBus.RabbitMQ
```

## Core concepts

- **`IConsumer<TMessage>`** — implement this for each message type you handle.
- **`IMessageBus.PublishAsync`** — fan-out to every registered consumer for a message type
  (zero consumers is not an error).
- **`IMessageBus.SendAsync`** — point-to-point; throws if zero or more than one consumer is
  registered for the message type.
- **`IRequestClient<TRequest, TResponse>`** — sends a request to exactly one
  `IRequestHandler<TRequest, TResponse>` and awaits its correlated response.
- **Retry** — every consumer invocation runs through a Polly retry pipeline (exponential
  backoff, 3 attempts by default). Configure with `cfg.UseRetry(retryCount, baseDelay)`.
- **Transports** — `InMemoryTransport` (default, in-process dispatch) or
  `RabbitMqTransport`/`RabbitMqConsumerHost` (cross-process, via `Swevo.AutoBus.RabbitMQ`).

## RabbitMQ transport

```csharp
services.AddAutoBus(cfg => cfg.AddConsumer<SendWelcomeEmail>());
services.AddAutoBusRabbitMq(options =>
{
    options.HostName = "localhost";
    options.QueuePrefix = "orders-service"; // scopes queue names per service
});
```

Publishing a message sends it to a fanout exchange named after the message's full type name.
`AddAutoBusRabbitMq` also registers a hosted `RabbitMqConsumerHost` that declares and binds a
durable queue for every locally-registered consumer, so the same `IConsumer<T>` code runs
whether messages arrive in-process or over the broker — call `AddAutoBusRabbitMq` *after*
`AddAutoBus` since it replaces the default in-memory transport.

## Request/Response

If you're migrating from MassTransit, AutoBus now supports the familiar "send a request and
await a correlated reply" pattern for in-process modular-monolith calls:

```csharp
public sealed record GetOrderStatus(int OrderId);
public sealed record OrderStatus(string Value);

public sealed class GetOrderStatusHandler : IRequestHandler<GetOrderStatus, OrderStatus>
{
    public Task<OrderStatus> Consume(ConsumeContext<GetOrderStatus> context)
        => Task.FromResult(new OrderStatus($"Order {context.Message.OrderId} is ready"));
}

services.AddAutoBus(cfg =>
{
    cfg.AddRequestHandler<GetOrderStatusHandler>();
    cfg.UseRequestTimeout(TimeSpan.FromSeconds(10)); // optional, default is 30s
});

var client = serviceProvider.GetRequiredService<IRequestClient<GetOrderStatus, OrderStatus>>();
var response = await client.GetResponseAsync(new GetOrderStatus(42));
```

Conceptually this is similar to MassTransit's `IRequestClient<TRequest>.GetResponse<TResponse>()`,
but AutoBus keeps it intentionally small:

- **In-process only.** Request/response uses an in-memory correlation registry and local handler
  dispatch; it is not a distributed RPC abstraction across RabbitMQ or other transports.
- **Exactly one handler.** `GetResponseAsync` throws if zero or more than one matching
  `IRequestHandler<TRequest, TResponse>` is registered.
- **Same retry pipeline.** Request handlers execute through the same Polly-backed retry pipeline
  as regular `IConsumer<T>` deliveries.
- **Correlated replies.** Every request gets a correlation ID exposed as
  `ConsumeContext<TRequest>.CorrelationId`.
- **Clear timeout behavior.** If no response arrives before the effective timeout, AutoBus throws
  `RequestTimeoutException`.

## Using AutoBus from an EF Core transactional outbox

AutoBus's `IMessageBus.PublishAsync(object message, Type messageType, CancellationToken)`
overload mirrors MassTransit's `IPublishEndpoint.Publish(object, Type, CancellationToken)`
signature, so it's a drop-in replacement inside an outbox processor loop — see
[EFCore.Outbox](https://github.com/Swevo/EFCore.Outbox), which uses AutoBus instead of
MassTransit for exactly this reason.

## Design goals

- **MIT licensed, forever.** No commercial tier, no per-seat fees.
- **Small surface area.** Consumers, publish/send, retry — no bespoke DSL to learn.
- **Same code, two transports.** Consumers are written once; swapping `InMemoryTransport` for
  `RabbitMqTransport` is a one-line DI change.

## Roadmap

- Saga/state-machine support and additional broker transports (Azure Service Bus, Amazon SQS)
  are being considered for future releases, scoped to real demand rather than parity with
  MassTransit's full feature set.

## 💼 Need .NET consulting?

I'm the author of AutoBus and a suite of compile-time source generators
([AutoWire](https://github.com/Swevo/AutoWire), [AutoMap.Generator](https://github.com/Swevo/AutoMap.Generator))
and 28+ Polly v8 resilience packages. I'm available for consulting on **Polly v8 resilience**,
**Azure cloud architecture**, and **clean .NET design**.

**[→ solidqualitysolutions.com](https://www.solidqualitysolutions.com/)** · **[LinkedIn](https://www.linkedin.com/in/justbannister/)**

## Also by the same author

> 🌐 Full suite overview: **[swevo.github.io](https://swevo.github.io/)**

| Package | Description |
|---|---|
| [**FluentPdf**](https://github.com/Swevo/FluentPdf) | Free, MIT-licensed fluent PDF generation — alternative to QuestPDF's commercial license. |
| [**AutoArchitecture**](https://github.com/Swevo/AutoArchitecture) | Free, MIT-licensed compile-time architecture rule enforcement — alternative to NDepend. |
| [**EFCore.Outbox**](https://github.com/Swevo/EFCore.Outbox) | Transactional outbox pattern for EF Core — pairs naturally with AutoBus. |
| [**Swevo.AutoAssert**](https://github.com/Swevo/AutoAssert) | Free, MIT-licensed fluent assertions — alternative to FluentAssertions' commercial license. |
| [**EFCore.BulkOperations**](https://github.com/Swevo/EFCore.BulkOperations) | Free, MIT-licensed bulk insert/update/delete for EF Core. |
| [**AutoWire**](https://github.com/Swevo/AutoWire) | Compile-time DI auto-registration — `[Scoped]`/`[Singleton]`/`[Transient]` generates `IServiceCollection` registration code. |
| [**AutoDispatch.Generator**](https://github.com/Swevo/AutoDispatch.Generator) | Compile-time CQRS dispatcher — free alternative to MediatR's commercial license. |
| [**PollyAnalyzers**](https://github.com/Swevo/PollyAnalyzers) | Free Roslyn analyzers for async/resilience anti-patterns — blocking calls, async void, fire-and-forget tasks, swallowed exceptions. |
| [**PollyAction**](https://github.com/Swevo/PollyAction) | Free retry/backoff GitHub Action — wrap any CI step with exponential-backoff retries. |

## License

MIT © Justin Bannister
