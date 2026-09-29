# Components.Messaging

Broker-agnostic interfaces for publishing and consuming integration events, plus the generic runtime pieces of consumption that don't depend on any specific broker.

```csharp
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(
        TEvent @event,
        string? destination = null,
        string? messageKey = null,
        CancellationToken cancellationToken = default) where TEvent : class;
}

public interface IEventHandler<in TEvent> where TEvent : class
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
```

## Why

The Application/Infrastructure layers depend only on these abstractions, never on RabbitMQ, SQS, SNS, or any other transport directly. Swapping the broker means swapping which concrete package is registered in DI — no change to any use case that publishes an event, or any handler that reacts to one.

### `IEventPublisher` — why `destination`/`messageKey`, and no exchange-type methods

- **No exchange type, no queue-vs-topic method split.** "Direct/topic/fanout exchange" is AMQP vocabulary — SQS has no exchange concept at all (you publish straight to a queue), and SNS filters via message attributes, not wildcard routing keys. A `PublishFanout()`/`PublishTopic()` method on this interface would force every caller to think in RabbitMQ terms even when the app runs on SQS underneath. Whatever routing behavior a specific broker needs is a **configuration concern of that broker's own package**, not something exposed here.
- **`destination` (optional)** — the one thing every messaging system has: *some* addressable target for a published message (an exchange name, a queue URL, a topic ARN). Left `null`, a concrete implementation falls back to whatever default it was configured with at startup. Passed explicitly, it overrides that default for a single call — e.g. a multi-tenant app choosing the destination per request.
- **`messageKey` (optional)** — a generic hint used *within* a destination to route, group, or partition a message. Different transports interpret it differently (RabbitMQ: routing key; SQS FIFO: `MessageGroupId`; SNS: `MessageGroupId` or a filterable attribute). Left `null`, the RabbitMQ implementation falls back to the event's type name.

### `IEventHandler<TEvent>` — the consumption-side counterpart

Whoever reacts to a consumed event implements this — one method, no broker knowledge. `in TEvent` is contravariant, the same convention `MediatR`'s `INotificationHandler<in TNotification>` uses.

## `EventConsumer<TEvent>` — the generic runtime shell for consumption

```csharp
public class EventConsumer<TEvent>(
    IEventConsumerStartup<TEvent> startup,
    IServiceScopeFactory scopeFactory) : BackgroundService where TEvent : class
```

A `BackgroundService`: on start, it calls into a broker-specific `IEventConsumerStartup<TEvent>` (declare topology, begin consuming), handing it a callback (`InvokeHandlerAsync`) to invoke for every message received; then it just idles until the host shuts down. The callback creates a **fresh `IServiceScope` per message** and resolves `IEventHandler<TEvent>` from it — needed because a handler may depend on scoped services (e.g. a repository holding a DB connection), and there's no per-request scope here the way there would be in a web app.

```csharp
public interface IEventConsumerStartup<TEvent> where TEvent : class
{
    Task StartupAsync(Func<TEvent, CancellationToken, Task> handleEvent, CancellationToken cancellationToken);
}
```

This interface is where the actual broker mechanics live (topology declaration, the receive loop, ack/nack) — `EventConsumer<TEvent>` itself never touches a queue, an exchange, or any transport-specific type. [`Components.Messaging.RabbitMQ`](../Components.Messaging.RabbitMQ/README.md)'s `RabbitMqConsumerStartup<TEvent>` is the concrete implementation.

`EventConsumerOptions` (`Destination`, `MessageKey`, `PrefetchCount`) is the configuration shape a broker package binds **per event type** — see the RabbitMQ package's README for how it's read from `appsettings.json`.

## Concrete implementations

- [`Components.Messaging.RabbitMQ`](../Components.Messaging.RabbitMQ/README.md) — RabbitMQ, via `RabbitMQ.Client`.
- SQS/SNS implementations: not built yet — these abstractions are designed to accommodate them without changes (see above), but there's no concrete package for either in this repository yet.

## Tests and sample

None here on purpose — same reasoning as `Components.SQLRepository`: interfaces and a generic shell have no broker-specific logic to exercise on their own. `Components.Messaging.RabbitMQ.Tests` and `.Sample` are what exercise this abstraction in practice.

See the [components root README](../../README.md) for build/pack/test commands that apply to every package in this solution.
