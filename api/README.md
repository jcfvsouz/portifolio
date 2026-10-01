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
│   ├── Promo.Api.Domain/
│   │   ├── Entities/                   # Tenant, BuyerGroup, Buyer, Campaign
│   │   ├── Enums/                      # CampaignStatus
│   │   ├── Repositories/               # ICampaignRepository, IBuyerGroupRepository (contracts only)
│   │   ├── Services/                   # IPublishCampaignService/PublishCampaignService — see below
│   │   └── AssemblyInfo.cs             # InternalsVisibleTo("Promo.Api.Infrastructure") — see Infrastructure below
│   ├── Promo.Api.Application/
│   │   ├── UseCases/                   # CreateCampaignUseCase, PublishCampaignUseCase
│   │   ├── Notifications/              # ICampaignPublishedNotifier (contract only) — see Infrastructure below
│   │   ├── Requests/                   # CreateCampaignRequest
│   │   ├── Validators/                 # CreateCampaignRequestValidator
│   │   └── ApplicationModule.cs        # IStartup — registers the validator, the service, and both use cases
│   ├── Promo.Api.Infrastructure/
│   │   ├── Repositories/               # CampaignRepository, BuyerGroupRepository (Dapper)
│   │   ├── Persistence/                # CampaignRow, BuyerGroupRow — see below for why they exist
│   │   ├── Notifications/              # CampaignPublished (internal), CampaignPublishedNotification — see below
│   │   ├── AssemblyInfo.cs             # InternalsVisibleTo("Promo.Api.Infrastructure.Tests")
│   │   └── InfrastructureModule.cs     # IStartup — registers SQL + repositories + notifier + composes RabbitMqPublisherModule
│   └── Promo.Api/
│       ├── Endpoints/                  # CampaignEndpoints, AuthEndpoints (extension methods mapping routes)
│       ├── Configuration/               # SwaggerModule, JwtAuthenticationModule, ExceptionHandlingModule (all IStartup)
│       ├── ExceptionHandling/           # GlobalExceptionHandler
│       ├── Program.cs
│       └── appsettings.json
└── test/
    ├── Promo.Api.Domain.Tests/
    │   ├── Entities/                   # CampaignTests
    │   ├── Fixtures/                   # PublishCampaignServiceFixture
    │   └── Services/                   # PublishCampaignServiceTests
    ├── Promo.Api.Application.Tests/
    │   ├── Builders/                    # Bogus-backed test data — mirrors components/'s TestSupport pattern
    │   ├── Fixtures/                    # Moq fixtures — see the components root README for the pattern
    │   ├── UseCases/                    # CreateCampaignUseCaseTests, PublishCampaignUseCaseTests
    │   └── Validators/                  # CreateCampaignRequestValidatorTests
    └── Promo.Api.Infrastructure.Tests/
        └── Notifications/               # CampaignPublishedNotificationTests (+ its Fixture)
```

Every namespace mirrors its folder path 1:1 (`Promo.Api.Application.UseCases` lives in `Promo.Api.Application/UseCases/`, and so on) — same convention `components/`'s `Builders/`/`Fixtures/` folders already use.

## Domain

`Tenant`, `BuyerGroup`, `Buyer`, `Campaign` — plain classes with a public constructor for creation, so nothing outside the entity can put it in an invalid state through construction. `Campaign.Publish()` is the one real business rule: it refuses to publish an already-published campaign, returning [`Result`](../components/src/Components.Result/README.md) instead of throwing — same "business-rule failures are never exceptions" convention as the rest of the repo.

`Campaign` and `BuyerGroup` use `internal set` rather than `private set` on their properties — not `private`, because Infrastructure needs to rebuild an entity's exact persisted state (`Id` included) on read, and there's no public constructor shaped for that. `internal` still keeps every other layer (Application, the API host) locked out — only `Promo.Api.Infrastructure` gets that access, via `InternalsVisibleTo` in `AssemblyInfo.cs`. See [Infrastructure](#infrastructure) for where it's actually used.

`ICampaignRepository`/`IBuyerGroupRepository` and `IPublishCampaignService`/`PublishCampaignService` live here too, not in Application: a repository contract is part of the domain model (it's phrased entirely in terms of `Campaign`/`BuyerGroup`), and `PublishCampaignService` is a domain service — it enforces `Campaign.Publish()`'s invariant and nothing else, with no notion of HTTP, validation, or messaging. Infrastructure implements the repositories; `ApplicationModule` registers `PublishCampaignService` itself, since Domain is a plain class library with no `IStartup` of its own.

```csharp
public class PublishCampaignService(ICampaignRepository campaignRepository) : IPublishCampaignService
{
    public async Task<Result<Campaign>> PublishAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await campaignRepository.GetByIdAsync(campaignId, cancellationToken);
        if (campaign is null)
            return Result<Campaign>.Error($"Campaign '{campaignId}' was not found.");

        var publishResult = campaign.Publish();
        if (!publishResult.Success)
            return Result<Campaign>.Error(publishResult.ErrorMessage);

        await campaignRepository.UpdateAsync(campaign, cancellationToken);
        return Result<Campaign>.Ok(campaign);
    }
}
```

## Application

- `CreateCampaignUseCase` — validates the request (FluentValidation), loads the target `BuyerGroup` to confirm it exists and to derive its `TenantId`, creates the `Campaign`, persists it. Returns `Result<Guid>` (the new campaign's id).
- `ICampaignPublishedNotifier` — a Dependency Inversion: Application only declares "something can be told a campaign was published" (`NotifyAsync(Campaign, CancellationToken)`); it has no idea RabbitMQ, or any broker, exists. Infrastructure implements it — see below.
- `PublishCampaignUseCase` — orchestration only: calls `IPublishCampaignService.PublishAsync` (Domain's service), and if that succeeded, calls `ICampaignPublishedNotifier.NotifyAsync` with the updated campaign. That's its entire job — it depends on neither `ICampaignRepository` nor any messaging type directly, only the Domain service contract and the Application-level notifier abstraction. Returns `Result`.
- `ApplicationModule : IStartup` — registers the validator, `IPublishCampaignService -> PublishCampaignService` (Domain's own type — Application owns this registration only because Domain has no `IStartup` to do it itself), and both use cases. `ICampaignPublishedNotifier` is registered by `InfrastructureModule`, since that's the layer providing its implementation.

## Infrastructure

`CampaignRepository` and `BuyerGroupRepository` — Dapper queries against [`ISqlRepository`](../components/src/Components.SQLRepository/README.md), same abstraction `Components.SQLServerRepository` implements.

**The one non-obvious piece here:** Dapper's default materialization needs a **public** parameterless constructor and **public** setters — Domain's entities deliberately have neither. So a `CampaignRow`/`BuyerGroupRow` — a plain, publicly-settable shape with no behavior — is what Dapper actually reads a row into. Each row declares an **explicit conversion operator** to its entity (`public static explicit operator Campaign(CampaignRow row)`), built with an object initializer against the entity's `internal set` properties:

```csharp
public static explicit operator Campaign(CampaignRow row) =>
    new(row.TenantId, row.BuyerGroupId, row.Name) { Id = row.Id, Status = (CampaignStatus)row.Status, PublishedAtUtc = row.PublishedAtUtc };
```

No dedicated reconstitution constructor needed in Domain — the operator is what "knows how to rebuild a `Campaign` from storage," and it lives in Infrastructure (where `CampaignRow` is declared), not Domain, keeping the dependency pointed the right way. The repository then just casts: `row is null ? null : (Campaign)row`.

`Campaign.Status` (`CampaignStatus` enum) is stored as a plain `INT` column matching the enum's underlying values — Dapper binds an enum parameter as its numeric value by default, so this needs zero custom type handling in either direction.

`CampaignPublishedNotification : ICampaignPublishedNotifier` is where the `CampaignPublished` event actually gets published — Application only knows it can `NotifyAsync(Campaign, CancellationToken)`; Infrastructure is the layer allowed to know both the `Campaign` entity and `Components.Messaging`'s `IEventPublisher`, so it owns the mapping from one to the other:

```csharp
public class CampaignPublishedNotification(IEventPublisher eventPublisher) : ICampaignPublishedNotifier
{
    public Task NotifyAsync(Campaign campaign, CancellationToken cancellationToken = default) =>
        eventPublisher.PublishAsync(
            new CampaignPublished(campaign.Id, campaign.TenantId, campaign.BuyerGroupId, campaign.Name, campaign.PublishedAtUtc!.Value),
            cancellationToken: cancellationToken);
}
```

`CampaignPublished` itself — the event record actually put on the bus — lives here too, as an `internal record`, not in Application: nothing outside Infrastructure needs to know its shape, only that *something* gets notified. Making it constructible from tests without exposing it publicly is what `AssemblyInfo.cs`'s `[assembly: InternalsVisibleTo("Promo.Api.Infrastructure.Tests")]` is for.

`InfrastructureModule : IStartup` registers `AddSqlServerRepository()`, both repositories, and `ICampaignPublishedNotifier -> CampaignPublishedNotification`, then **composes** with `Components.Messaging.RabbitMQ`'s own `RabbitMqPublisherModule` (`new RabbitMqPublisherModule().ConfigureServices(services, configuration)`) instead of calling `AddRabbitMqPublisher(configuration)` directly — reusing the existing module is the same idea as the DI extension methods it wraps, just one level up.

The repositories still have no dedicated tests — there's no logic here to isolate from a real database (each method is a direct Dapper call), so correctness there is proven by actually running the host, the same way `Components.SQLServerRepository.Sample` proves its wiring without a database-touching unit test. `CampaignPublishedNotification` is different: it's pure mapping logic with a mockable dependency, so it gets a real unit test in `Promo.Api.Infrastructure.Tests`, following the same Moq + `IClassFixture` pattern as everything else in this repo.

## The host

[`Promo.Api`](src/Promo.Api/README.md) is the only `Microsoft.NET.Sdk.Web` project here — it wires Domain/Application/Infrastructure together via `UseStartup<T>()`, and owns everything HTTP-specific: endpoint routing, Swagger, JWT auth, global exception handling. See its own README for the full detail (modules, endpoints, configuration, and how to run it and exercise it end to end).

## Tests

```bash
cd api
dotnet test Promo.Api.slnx
```

`Promo.Api.Domain.Tests`, `Promo.Api.Application.Tests`, and `Promo.Api.Infrastructure.Tests` all follow this repo's standard pattern — Moq + `IClassFixture<TFixture>` per class under test, `ConfigureMocks()`/`NewInstance()`, named `Setup_{Dependency}_{Method}_Result(...)` methods, Bogus-backed builders for test data — see the [components root README](../components/README.md#tests) for the full rationale. `Promo.Api.Infrastructure.Tests` is the one project here testing a type Infrastructure keeps `internal` (`CampaignPublishedNotification`'s `CampaignPublished` event) — made visible to it only, via `[assembly: InternalsVisibleTo("Promo.Api.Infrastructure.Tests")]`.
