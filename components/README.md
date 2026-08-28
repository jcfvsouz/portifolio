# Components

Shared, reusable building blocks for the Campaign Publication & Batch Email Dispatch pipeline (see the [project root README](../README.md) for the full picture). Each folder under `src/` is an independent class library, packed as its own NuGet package and consumed by the API and Worker via `PackageReference` — not `ProjectReference` — the same way you'd pull in a package built by another team.

## Why a separate solution, packed as NuGet, instead of just project references

Project references are the easy path, but they hide a real discipline that shows up constantly in production codebases with a shared-library team: **versioning**. If more than a project depends on `Components.Result`, and one of then needs a new field that other doesn't need yet, a project reference lets that change ripple into the consumer's build silently. A package reference forces an explicit version bump and an explicit upgrade decision on each consumer's side.

This solution exists to practice exactly that: build a component, pack it, version it, and have consuming apps opt into upgrades deliberately.

## Layout

```
components/
├── Components.slnx                    # solution file (new .slnx format, .NET 10 SDK default)
├── Directory.Build.props              # settings shared by every project below: target framework,
│                                       # nullable/implicit usings, and NuGet pack settings
├── nuget.config                       # tells dotnet restore to look in ./nupkgs first, nuget.org second
├── nupkgs/                            # local package feed — output of `dotnet build`/`dotnet pack`
├── src/
│   ├── Components.Result/             # Result / Result<T> — explicit success/failure, no exceptions
│   ├── Extensions.FluentResult/       # ValidationResult -> Result mapping helpers
│   ├── Components.SQLRepository/      # ISqlRepository — engine-agnostic IDbConnection wrapper
│   └── Components.SQLServerRepository/# SQL Server implementation of ISqlRepository (Dapper + Microsoft.Data.SqlClient)
├── test/
│   ├── Components.TestSupport/            # shared, non-published test helpers
│   │   ├── Builders/                          # ValidationResultBuilder, TestConfigurationBuilder
│   │   └── Fixtures/                           # ValidationResultFixture, SqlServerConfigurationFixture (IClassFixture<T>)
│   ├── Components.Result.Tests/
│   ├── Extensions.FluentResult.Tests/
│   └── Components.SQLServerRepository.Tests/
└── samples/
    ├── Components.Result.Sample/              # console app: every Result/Result<T> factory method, printed
    ├── Extensions.FluentResult.Sample/         # console app: FluentValidation -> ToResult() on a valid/invalid DTO
    └── Components.SQLServerRepository.Sample/ # console app: real DI wiring + a live SELECT 1 round trip
```

`Components.SQLRepository` has no test project or sample — it's a single interface with no logic to exercise or demonstrate on its own; `Components.SQLServerRepository.Sample` is what exercises it in practice.

## Packages

### `Portfolio.Components.Result`
`Result` and `Result<T>` are simple wrapper types that make a use case's outcome explicit: `Success`, `ErrorMessage`, and (for `Result<T>`) `Content`. The convention across this codebase is that **business-rule failures are never exceptions** — a use case returns `Result<T>.Error(...)` and the caller (a controller, a worker handler) decides what to do with it. Exceptions are reserved for actually-exceptional situations (a dropped DB connection, a bug), not for "the campaign name was empty."

### `Portfolio.Extensions.FluentResult`
Two extension methods, `ToResult()` and `ToResult<T>(content)`, that turn a FluentValidation `ValidationResult` directly into a `Result` / `Result<T>`. Without this, every use case would hand-roll the same `if (!validationResult.IsValid) { ... }` mapping. With it, a use case does:

```csharp
var validation = await _validator.ValidateAsync(dto);
if (!validation.IsValid) return validation.ToResult<CampaignDto>();
```

### `Portfolio.Components.SQLRepository`
Just one interface: `ISqlRepository`, exposing an `IDbConnection Connection`. It intentionally knows nothing about SQL Server, SQLite, Postgres, or any other engine — that's the point. The Application/Infrastructure layers depend only on this abstraction, and Dapper queries run against `ISqlRepository.Connection` regardless of which engine is plugged in underneath.

### `Portfolio.Components.SQLServerRepository`
The concrete implementation of `ISqlRepository` for SQL Server, using `Microsoft.Data.SqlClient`. It reads its connection string from configuration key `ConnectionStrings:SqlServer`, and registers itself with `services.AddSqlServerRepository()`. Ships `Dapper` as a package dependency so any project referencing this package gets Dapper's `IDbConnection` extension methods (`Query`, `Execute`, ...) for free.

## Building and packing

```bash
cd components
dotnet build Components.slnx
```

That's it — `GeneratePackageOnBuild` is set in `Directory.Build.props`, so every `dotnet build` also runs `dotnet pack` under the hood and drops the `.nupkg` files into `nupkgs/`. There's no separate "pack" step to remember.

To bump a package version, edit `<PackageVersion>` in `Directory.Build.props` (or override it per-project) and rebuild — the new version appears alongside the old one in `nupkgs/`, so consumers upgrade on their own schedule instead of being forced.

## Tests

Every package with actual logic has an xUnit test project under `test/`, testing the source directly via `ProjectReference` (not the packed `.nupkg` — packaging is a build concern, not something the tests need to go through). Assertions use **FluentAssertions** (`result.Success.Should().BeTrue()`) instead of `Assert.*`, and every test body is split into explicit `// Arrange` / `// Act` / `// Assert` comments (AAA) so the three phases of a test are visually obvious at a glance, even in a test with no meaningful arrange step.

> FluentAssertions is pinned to **7.2.2** — the last release under the free Apache 2.0 license, before v8 introduced a commercial license. For a portfolio repository meant to be cloned and run by anyone, that matters.

Two reusable test patterns live in the shared, unpublished `Components.TestSupport` project, organized into their own folders so the pattern is obvious from the file tree, not just from convention:

- **`Builders/`** (`ValidationResultBuilder`, `TestConfigurationBuilder`) — fluent classes that construct a valid test input (a `ValidationResult`, an `IConfiguration`) step by step, so a test's arrange section reads as a sentence (`new ValidationResultBuilder().WithError("Name", "Name is required").Build()`) instead of hand-assembling a `List<ValidationFailure>` inline.
- **`Fixtures/`** (`ValidationResultFixture`, `SqlServerConfigurationFixture`) — plain classes consumed via xUnit's `IClassFixture<T>`. xUnit constructs the fixture once per test class and injects the same instance into every test method in that class, so shared, expensive-ish setup (building a validation result, building a configuration object) happens once instead of being repeated in every `[Fact]`.

Both namespace to their folder (`Components.TestSupport.Builders`, `Components.TestSupport.Fixtures`), so a consuming test project's `using` statements say exactly which kind of helper it's pulling in.

Run everything with:

```bash
cd components
dotnet test Components.slnx
```

No database, broker, or network call is involved — `Components.SQLServerRepository.Tests` only exercises connection-string wiring and object lifetime (is the right `IDbConnection` type constructed, is it cached, does `Dispose` behave) against an in-memory `IConfiguration`; it never opens a real connection to SQL Server.

## Samples

Each package with runnable behavior has a console app under `samples/`, referencing the component by `ProjectReference` — same reasoning as the test projects: run/debug against the source directly, not a packed `.nupkg`. These aren't tests (no assertions, no CI value); they exist so you can `dotnet run` a component in isolation, read what it prints, step through it in a debugger, and see with your own eyes that a change didn't break the behavior — before wiring it into the full API/Worker pipeline.

- **`Components.Result.Sample`** — calls every factory method (`Ok`, `Error(string)`, `Error(List<ValidationFailure>)`, both for `Result` and `Result<T>`) and prints the resulting `Success`/`ErrorMessage`/`Content`, including a tiny simulated use case to show the intended calling convention.
- **`Extensions.FluentResult.Sample`** — defines a throwaway `CampaignDto` + FluentValidation validator, validates one valid and one invalid instance, and prints what `ToResult()`/`ToResult<T>()` produce for each.
- **`Components.SQLServerRepository.Sample`** — the most useful one for hands-on debugging: builds a real `IConfiguration` from `appsettings.json` (+ environment variable override), registers `AddSqlServerRepository()`, resolves `ISqlRepository` through actual DI, and attempts a live `SELECT 1` via Dapper. It prints which connection string it's using (password redacted) and, if the round trip fails, a friendly explanation rather than a raw stack trace — useful both before the `docker/` SQL Server container exists and afterward, to confirm the container's credentials actually match.

Run any of them with:

```bash
cd components
dotnet run --project samples/Components.Result.Sample
dotnet run --project samples/Extensions.FluentResult.Sample
dotnet run --project samples/Components.SQLServerRepository.Sample
```

## Consuming these packages

The consumers projects should point their own `nuget.config` at this `nupkgs/` folder as a package source, then reference these libraries exactly like any NuGet.org package:

```xml
<PackageReference Include="Portfolio.Components.Result" Version="1.0.0" />
```

No `dotnet nuget push`, no NuGet.org account, no API key — a folder on disk is a perfectly valid NuGet feed, which is what makes this workflow practical to demonstrate end-to-end locally.
