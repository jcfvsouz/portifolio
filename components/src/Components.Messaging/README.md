# Components.Messaging

`IEventPublisher` — a single interface, broker-agnostic on purpose:

```csharp
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(
        TEvent @event,
        string? destination = null,
        string? messageKey = null,
        CancellationToken cancellationToken = default) where TEvent : class;
}
```

## Why

Same discipline as [`Components.SQLRepository`](../Components.SQLRepository/README.md): the Application/Infrastructure layers depend only on this abstraction, never on RabbitMQ, SQS, SNS, or any other transport directly. Swapping the broker means swapping which concrete package is registered in DI — no change to any use case that publishes an event.

That constraint shaped every parameter on purpose:

- **No exchange type, no queue-vs-topic method split.** "Direct/topic/fanout exchange" is AMQP vocabulary — SQS has no exchange concept at all (you publish straight to a queue), and SNS filters via message attributes, not wildcard routing keys. A `PublishFanout()`/`PublishTopic()` method on this interface would force every caller to think in RabbitMQ terms even when the app runs on SQS underneath. Whatever routing behavior a specific broker needs (which exchange type, durability, etc.) is a **configuration concern of that broker's own package** (e.g. `RabbitMqOptions` in [`Components.Messaging.RabbitMQ`](../Components.Messaging.RabbitMQ/README.md)), not something exposed here.
- **`destination` (optional)** — the one thing every messaging system has: *some* addressable target for a published message (an exchange name, a queue URL, a topic ARN). Left `null`, a concrete implementation falls back to whatever default it was configured with at startup (the common case — one exchange/topic per bounded context). Passed explicitly, it overrides that default for a single call — the case a fixed DI-time configuration can't cover, e.g. a multi-tenant app choosing the destination per request.
- **`messageKey` (optional)** — a generic hint used *within* a destination to route, group, or partition a message. Different transports interpret it differently (RabbitMQ: routing key; SQS FIFO: `MessageGroupId`; SNS: `MessageGroupId` or a filterable attribute) — the interface doesn't assume any one of them. Left `null`, the RabbitMQ implementation falls back to the event's type name.

## Concrete implementations

- [`Components.Messaging.RabbitMQ`](../Components.Messaging.RabbitMQ/README.md) — RabbitMQ, via `RabbitMQ.Client`.
- SQS/SNS implementations: not built yet — this interface is designed to accommodate them without changes (see above), but there's no concrete package for either in this repository yet.

## Tests and sample

None here on purpose — same reasoning as `Components.SQLRepository`: a single interface has no logic to exercise on its own. `Components.Messaging.RabbitMQ.Tests` and `.Sample` are what exercise this abstraction in practice.

See the [components root README](../../README.md) for build/pack/test commands that apply to every package in this solution.
