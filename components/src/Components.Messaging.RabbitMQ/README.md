# Components.Messaging.RabbitMQ

The RabbitMQ implementation of [`Components.Messaging`](../Components.Messaging/README.md)'s publish/consume abstractions, backed by `RabbitMQ.Client` 7.2.2 (the fully-async client).

> **Status:** publish and consume both work, including a dead-letter queue for permanently failing messages.

## Configuration

Broker connection settings bind from section **`RabbitMq`**, via the Options pattern (not a single connection string — RabbitMQ needs several independent values):

```json
{
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "ExchangeName": "campaign-pipeline"
  }
}
```

`ExchangeName` is the **default** destination used when a publish call doesn't pass one explicitly.

Each **consumer** additionally binds its own settings from `Consumers:{EventTypeName}` — see below.

## DI

The two extension methods below do the actual `IServiceCollection` registration; a shared, private `AddRabbitMqConnection` helper (idempotent via `TryAddSingleton`) registers the `IConnection` singleton once, however many times either is called:

```csharp
services.AddRabbitMqPublisher(configuration);
services.AddRabbitMqConsumer<CampaignPublished, CampaignPublishedHandler>(configuration);
```

Opening the TCP + AMQP handshake is expensive, so the whole app shares **one connection**, created via `ConnectionFactory.CreateConnectionAsync().GetAwaiter().GetResult()` — a deliberate sync-over-async, acceptable because it runs exactly once, at first resolution, not on a hot path.

### Via `Components.Hosting`'s `UseStartup<T>()`

Rather than calling those extension methods directly in `Program.cs`, this package ships `IStartup` wrappers ([`Components.Hosting`](../Components.Hosting/README.md)) so messaging's registration is one named, discoverable step:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.UseStartup<RabbitMqPublisherModule>();
builder.UseStartup<RabbitMqConsumerModule<CampaignPublished, CampaignPublishedHandler>>();
```

## Publishing

`RabbitMqEventPublisher` opens **one channel lazily** on first publish and caches it for its own lifetime, guarded by a `SemaphoreSlim` (creation is asynchronous, so a simple null-check isn't enough). Every distinct destination (exchange) it's asked to publish to gets declared **once** — a durable **topic exchange** (chosen over `direct`/`fanout` for wildcard-binding flexibility without losing exact matching) — tracked via a `ConcurrentDictionary<string, byte>` so a repeat publish to the same destination skips the round trip.

- `destination` → the exchange name; `null` falls back to the configured `ExchangeName`.
- `messageKey` → the AMQP routing key; `null` falls back to `typeof(TEvent).Name`.
- Serialization: `System.Text.Json`; message marked `Persistent = true`, `ContentType = "application/json"`.

```csharp
await publisher.PublishAsync(new CampaignPublished(campaignId, buyerGroupId));

await publisher.PublishAsync(                              // overriding both
    new CampaignPublished(campaignId, buyerGroupId),
    destination: $"tenant-{tenantId}-exchange",
    messageKey: "campaign.published.v1");
```

## Consuming

`RabbitMqConsumerStartup<TEvent>` implements `Components.Messaging`'s `IEventConsumerStartup<TEvent>` — it's what `EventConsumer<TEvent>` (the generic `BackgroundService`, see that package's README) calls into on startup:

1. Declares the exchange (idempotent — a consumer may start before any publisher ever has).
2. Declares a **dead-letter exchange** (fanout, `{Destination}.dlx`) and its **dead-letter queue** (`{Destination}.dlq`), bound together — see below.
3. Declares the main **queue**, durable, named from configuration, with an `x-dead-letter-exchange` argument pointing at the dead-letter exchange (survives a process restart with unprocessed messages intact).
4. **Binds** the main queue to the main exchange with a routing key (defaults to the event type name, same convention as publishing).
5. Sets **QoS/prefetch** — how many unacknowledged messages the broker will push at once. Without a limit, a slow consumer can be flooded; a low prefetch also enables fair dispatch across multiple instances of the same consumer.
6. Subscribes with **manual ack**: success → `BasicAck`; a handler exception (or a message body that fails to deserialize) → logged, then `BasicNack` with `requeue: false` (auto-ack was rejected here because it marks a message delivered *before* processing, losing it silently on a crash mid-handling).

### Dead-lettering, not discarding

A `BasicNack` with `requeue: false` doesn't have to mean "gone" — RabbitMQ automatically re-routes a nacked (or rejected, or TTL-expired) message to whatever exchange the queue's `x-dead-letter-exchange` argument names, with **no publish call of our own required**. That's why the dead-letter exchange and queue are declared *before* the main queue: the main queue's declaration already references the dead-letter exchange by name. A permanently failing message ends up sitting in `{Destination}.dlq`, inspectable (via the management UI or a one-off consumer), instead of looping forever or vanishing. The dead-letter exchange is a plain **fanout** — with exactly one queue behind it, there's nothing for a routing key to select between.

Per-event-type configuration binds from `Consumers:{EventTypeName}` using .NET's **named options** (`IOptionsMonitor<EventConsumerOptions>.Get(typeof(TEvent).Name)`), since each event type consumed needs its own queue/routing key/prefetch:

```json
{
  "Consumers": {
    "CampaignPublished": {
      "Destination": "campaign-published-queue",
      "MessageKey": "CampaignPublished",
      "PrefetchCount": 10
    }
  }
}
```

## Tests and sample

- Tests: `test/Components.Messaging.RabbitMQ.Tests` — `IConnection`/`IChannel` are large interfaces (many members: events, `BasicPublishAsync`, `ExchangeDeclareAsync`, ...), so hand-writing fakes would be mostly boilerplate. Uses **NSubstitute** instead — a deliberate, scoped exception to this repo's usual "no mocking framework" style.
  - **Publisher:** channel created once and reused across publishes and across different destinations, each distinct destination's exchange declared exactly once, routing key defaults to the event type name and can be overridden, message is persistent JSON, `DisposeAsync` disposes the channel.
  - **Consumer:** main exchange/dead-letter exchange/dead-letter queue/main queue all declared with the right arguments and bindings, routing key defaults to the event type name and can be overridden, QoS uses the configured prefetch count, `BasicConsumeAsync` starts with manual ack. Delivery itself is exercised by capturing the real `AsyncEventingBasicConsumer` passed into `BasicConsumeAsync` (via NSubstitute's `Arg.Do`) and calling its `HandleBasicDeliverAsync` directly — no broker needed to prove a received message is deserialized, dispatched, and acked, or nacked without requeue when the handler throws or the body fails to deserialize.
- Sample: `dotnet run --project samples/Components.Messaging.RabbitMQ.Sample` — real DI wiring via `UseStartup<RabbitMqPublisherModule>()`, builds `IConfiguration` from `appsettings.json` (+ environment variable override), and attempts a live publish. Prints the broker/exchange in use and a friendly explanation if the publish fails (no broker running yet), instead of a raw stack trace.

See the [components root README](../../README.md) for build/pack/test commands that apply to every package in this solution.
