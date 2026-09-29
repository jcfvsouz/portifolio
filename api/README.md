# Promo.Api

The Campaign Publication side of the pipeline (see the [project root README](../README.md) for the full picture): a Minimal API in Clean Architecture that lets a marketer create a campaign and publish it, emitting a `CampaignPublished` event for the (future) Worker to pick up.

## Why four projects for two endpoints

The point of this piece isn't the two endpoints themselves — it's demonstrating the boundary discipline Clean Architecture is actually for: **Domain** knows nothing outside itself, **Application** orchestrates but never touches SQL or a broker directly, **Infrastructure** is the only layer allowed to know Dapper or RabbitMQ exist, and the host wires everything together. Every dependency in this solution points inward — Infrastructure references Application, Application references Domain, never the other way around.

## Layout

```
api/
├── Promo.Api.slnx
├── Directory.Build.props          # net10.0, nullable/implicit usings — shared by every project below
├── nuget.config                   # nuget.org + ../components/nupkgs (this solution consumes those packages)
├── database/
│   ├── schema.sql                 # creates Tenants/BuyerGroups/Buyers/Campaigns
│   └── seed.sql                   # 1 tenant, 2 buyer groups, 5 buyers — fixed GUIDs, referenced below
├── src/
│   ├── Promo.Api.Domain/          # Tenant, BuyerGroup, Buyer, Campaign — no dependency on anything else here
│   ├── Promo.Api.Application/     # Use cases, DTOs, FluentValidation, CampaignPublished, repository interfaces
│   ├── Promo.Api.Infrastructure/  # Dapper repositories + RabbitMQ wiring — the only layer that knows either exists
│   └── Promo.Api/                 # Minimal API host — Program.cs, appsettings.json
└── test/
    ├── Promo.Api.Domain.Tests/
    └── Promo.Api.Application.Tests/
```

## Domain

`Tenant`, `BuyerGroup`, `Buyer`, `Campaign` — plain classes with a public constructor for creation and private setters, so nothing outside the entity can put it in an invalid state. `Campaign.Publish()` is the one real business rule: it refuses to publish an already-published campaign, returning [`Result`](../components/src/Components.Result/README.md) instead of throwing — same "business-rule failures are never exceptions" convention as the rest of the repo.

Each entity also has an `internal` constructor that takes every field, `Id` included — see [Infrastructure](#infrastructure) for why.

## Application

- `ICampaignRepository`, `IBuyerGroupRepository` — interfaces Domain-adjacent code depends on; Infrastructure implements them. This is the Dependency Inversion in "Clean Architecture": Application defines the contract, Infrastructure obeys it.
- `CreateCampaignUseCase` — validates the request (FluentValidation), loads the target `BuyerGroup` to confirm it exists and to derive its `TenantId`, creates the `Campaign`, persists it. Returns `Result<Guid>` (the new campaign's id).
- `PublishCampaignUseCase` — loads the campaign, calls `Campaign.Publish()`, persists the change, then publishes `CampaignPublished` via [`IEventPublisher`](../components/src/Components.Messaging/README.md). Returns `Result`.
- `CampaignPublished` — the integration event contract itself. It lives here (not in a separate shared package) because only `Promo.Api` publishes it today; if `worker/` ends up needing the same type, that's the point to extract it into something both projects reference — not before.
- `ApplicationModule : IStartup` — registers the validator and both use cases. See [`Components.Hosting`](../components/src/Components.Hosting/README.md) for what `IStartup`/`UseStartup<T>()` are.

## Infrastructure

`CampaignRepository` and `BuyerGroupRepository` — Dapper queries against [`ISqlRepository`](../components/src/Components.SQLRepository/README.md), same abstraction `Components.SQLServerRepository` implements.

**The one non-obvious piece here:** Dapper's default materialization needs a **public** parameterless constructor and **public** setters — Domain's entities deliberately have neither. So a `CampaignRow`/`BuyerGroupRow` — a plain, publicly-settable shape with no behavior — is what Dapper actually reads a row into; a `.ToDomain()` method on the row then calls the entity's `internal` all-fields constructor to produce the real `Campaign`/`BuyerGroup`. That constructor is `internal` specifically so only `Promo.Api.Infrastructure` can reach it (`Promo.Api.Domain`'s `AssemblyInfo.cs` grants it via `InternalsVisibleTo`) — Application and the API host still only ever see the constructor that enforces a fresh campaign starts as `Draft`.

`Campaign.Status` (`CampaignStatus` enum) is stored as a plain `INT` column matching the enum's underlying values — Dapper binds an enum parameter as its numeric value by default, so this needs zero custom type handling in either direction.

`InfrastructureModule : IStartup` registers `AddSqlServerRepository()` and both repositories, then **composes** with `Components.Messaging.RabbitMQ`'s own `RabbitMqPublisherModule` (`new RabbitMqPublisherModule().ConfigureServices(services, configuration)`) instead of calling `AddRabbitMqPublisher(configuration)` directly — reusing the existing module is the same idea as the DI extension methods it wraps, just one level up.

No dedicated test project — there's no logic here to isolate from a real database (each method is a direct Dapper call), so correctness is proven by actually running the host, the same way `Components.SQLServerRepository.Sample` proves its wiring without a database-touching unit test.

## The host (`Promo.Api`)

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.UseStartup<ApplicationModule>();
builder.UseStartup<InfrastructureModule>();
```

Two endpoints:

- `POST /campaigns` — body `{ "name": "...", "buyerGroupId": "..." }`. `201 Created` with the new id, or `400` with the validation/not-found error.
- `POST /campaigns/{id}/publish` — `200` on success, `400` if the campaign doesn't exist or is already published.

Configuration (`appsettings.json`): `ConnectionStrings:SqlServer` and a `RabbitMq` section, same shape as `Components.SQLServerRepository`/`Components.Messaging.RabbitMQ`'s own samples — there's no `docker/` compose file yet (that's a separate, still-planned piece of this project), so these point at a container that doesn't exist until you stand one up yourself.

### Verified by actually running it

- `POST /campaigns` with an empty `name` → `400`, with FluentValidation's own localized message (`'Name' deve ser informado.` in a pt-BR environment) — and it never touches the database, proving validation happens before any repository call.
- `POST /campaigns` with a valid body → reaches a real SQL Server round trip through Dapper and fails only at login (no matching `docker/` container yet) — confirming the whole chain (routing → DI → use case → repository → Dapper → `Microsoft.Data.SqlClient`) is wired correctly end to end.

## Running it yourself

```bash
cd api
dotnet build Promo.Api.slnx
dotnet run --project src/Promo.Api
```

Point `ConnectionStrings:SqlServer` at a real SQL Server, run `database/schema.sql` then `database/seed.sql` against it, and point `RabbitMq` at a real broker to exercise the full path. `database/seed.sql` uses fixed GUIDs on purpose:

- BuyerGroup `22222222-2222-2222-2222-222222222222` — VIP Customers
- BuyerGroup `33333333-3333-3333-3333-333333333333` — Newsletter Subscribers

```bash
curl -X POST http://localhost:<port-printed-on-startup>/campaigns \
  -H "Content-Type: application/json" \
  -d '{"name":"Black Friday","buyerGroupId":"22222222-2222-2222-2222-222222222222"}'
```

## Tests

```bash
cd api
dotnet test Promo.Api.slnx
```

`Promo.Api.Domain.Tests` and `Promo.Api.Application.Tests` follow this repo's standard pattern — Moq + `IClassFixture<TFixture>` per class under test, `ConfigureMocks()`/`NewInstance()`, named `Setup_{Dependency}_{Method}_Result(...)` methods, Bogus-backed builders for test data — see the [components root README](../components/README.md#tests) for the full rationale.
