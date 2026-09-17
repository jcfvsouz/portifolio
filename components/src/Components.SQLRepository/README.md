# Components.SQLRepository

`ISqlRepository` — a single interface, exposing one member:

```csharp
public interface ISqlRepository : IDisposable
{
    IDbConnection Connection { get; }
}
```

## Why

This package intentionally knows **nothing** about SQL Server, SQLite, Postgres, or any other engine — that's the entire point. The Application/Infrastructure layers depend only on this abstraction and run Dapper queries against `ISqlRepository.Connection`, regardless of which engine is plugged in underneath. Swapping the database engine means swapping which concrete package is registered in DI — no change to any query, repository, or use case that consumes `ISqlRepository`.

## Concrete implementations

- [`Components.SQLServerRepository`](../Components.SQLServerRepository/README.md) — SQL Server, via `Microsoft.Data.SqlClient` + Dapper.

## Tests and sample

None here on purpose — a single interface has no logic to exercise or demonstrate on its own. `Components.SQLServerRepository.Tests` and `.Sample` are what exercise this abstraction in practice.

See the [components root README](../../README.md) for build/pack/test commands that apply to every package in this solution.
