# Components.Messaging.RabbitMQ

The RabbitMQ implementation of [`IEventPublisher`](../Components.Messaging/README.md), backed by `RabbitMQ.Client` 7.2.2 (the fully-async client).

> **Status:** publisher only. A consumer (`IEventHandler<TEvent>` + a hosted background service) is planned but not built yet.

## Configuration

Bound from configuration section **`RabbitMq`**, via the Options pattern (not a single connection string — RabbitMQ needs several independent values, so a strongly-typed class is the idiomatic fit):

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

`ExchangeName` is the **default** destination used when a caller doesn't pass one explicitly to `PublishAsync` — see below.

## DI

```csharp
services.AddRabbitMqPublisher(configuration);
```

This does three things:
1. `services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"))`.
2. Registers `IConnection` as a **singleton** — opening the TCP + AMQP handshake is expensive, so the whole app shares one connection, created once via `ConnectionFactory.CreateConnectionAsync().GetAwaiter().GetResult()` (a deliberate sync-over-async: acceptable because it runs exactly once, at first resolution, not on a hot path).
3. Registers `IEventPublisher -> RabbitMqEventPublisher` as a singleton.

## How `RabbitMqEventPublisher` works

**Connection vs. channel.** A *connection* is the TCP/AMQP link to the broker (one per process, singleton — see above). A *channel* is a lightweight, multiplexed virtual connection where publishing/consuming actually happens — cheap to open, but not safe to share across concurrent publishes without care. `RabbitMqEventPublisher` opens **one channel lazily** on first publish and caches it for its own lifetime (same "expensive resource, cached" pattern as `SqlServerRepository`'s connection), guarded by a `SemaphoreSlim` rather than a simple null-check because the creation itself is asynchronous.

**Exchange declaration.** RabbitMQ requires an exchange to exist before you can publish to it — `ExchangeDeclareAsync` creates it if missing, and is a no-op (safe to call repeatedly) if it already exists with matching parameters. Every exchange this publisher has been asked to publish to gets declared **once**, the first time it's used, as a **durable topic exchange**; a `ConcurrentDictionary<string, byte>` tracks which destinations have already been declared on the current channel so a repeat publish to the same destination skips the round trip.

**Exchange type: topic.** Chosen over `direct` (exact-match only) and `fanout` (ignores routing entirely, always broadcasts) because it supports wildcard bindings (`*` = one segment, `#` = zero or more) without giving up exact matching when that's all a consumer needs — the least restrictive option that doesn't cost anything today.

**Destination and message key.**
- `destination` → the exchange name. `null` falls back to the configured `ExchangeName`; passing one overrides it for that call (e.g. per-tenant exchanges).
- `messageKey` → the AMQP routing key. `null` falls back to `typeof(TEvent).Name` — a consumer can bind a queue to a specific event type's name, or use a wildcard pattern to catch a family of events.

**Serialization.** `System.Text.Json`, message marked `Persistent = true` (survives a broker restart) with `ContentType = "application/json"`.

## Usage

```csharp
// Publishes to the default exchange (RabbitMq:ExchangeName), routing key = "CampaignPublished"
await publisher.PublishAsync(new CampaignPublished(campaignId, buyerGroupId));

// Overrides both: a per-tenant exchange, and a hierarchical routing key for topic bindings
await publisher.PublishAsync(
    new CampaignPublished(campaignId, buyerGroupId),
    destination: $"tenant-{tenantId}-exchange",
    messageKey: "campaign.published.v1");
```

## Tests and sample

- Tests: `test/Components.Messaging.RabbitMQ.Tests` — `IConnection`/`IChannel` are large interfaces (many members: events, `BasicPublishAsync`, `ExchangeDeclareAsync`, ...), so hand-writing fakes would be mostly boilerplate. Uses **NSubstitute** instead — a deliberate, scoped exception to this repo's usual "no mocking framework" style, chosen because the alternative (a hand-rolled fake implementing the full `IChannel` surface) trades real signal for busywork. Covers: channel created once and reused across publishes and across different destinations, each distinct destination's exchange declared exactly once, routing key defaults to the event type name and can be overridden, message is persistent JSON, `DisposeAsync` disposes the channel (and doesn't throw if none was ever created).
- Sample: `dotnet run --project samples/Components.Messaging.RabbitMQ.Sample` — real DI wiring, builds `IConfiguration` from `appsettings.json` (+ environment variable override), and attempts a live publish. Prints the broker/exchange in use and a friendly explanation if the publish fails (no broker running yet), instead of a raw stack trace.

See the [components root README](../../README.md) for build/pack/test commands that apply to every package in this solution.
