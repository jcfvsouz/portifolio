# Components.SQLServerRepository

The SQL Server implementation of [`ISqlRepository`](../Components.SQLRepository/README.md), backed by `Microsoft.Data.SqlClient`.

## How it works

```csharp
public class SqlServerRepository(IConfiguration configuration) : ISqlRepository
{
    public IDbConnection Connection { get; } // lazily creates and caches a SqlConnection
    public void Dispose();                   // closes and disposes it, if it was ever created
}
```

- Reads its connection string from configuration key **`ConnectionStrings:SqlServer`**.
- The connection is created lazily on first access to `Connection` and cached for the repository's lifetime — the constructor doesn't touch the network at all, only reads configuration.
- Ships `Dapper` as a package dependency, so any project referencing this package gets Dapper's `IDbConnection` extension methods (`Query`, `Execute`, ...) for free, without adding it as a separate reference.

## DI

```csharp
services.AddSqlServerRepository(); // registers ISqlRepository -> SqlServerRepository, scoped
```

Requires an `IConfiguration` already registered in the container (`services.AddSingleton(configuration)` or the ASP.NET Core host default) with a `ConnectionStrings:SqlServer` entry.

## Usage

```csharp
using var repository = serviceProvider.GetRequiredService<ISqlRepository>();
var result = repository.Connection.QuerySingle<int>("SELECT 1");
```

## Tests and sample

- Tests: `test/Components.SQLServerRepository.Tests` — connection-string wiring and object lifetime (right `IDbConnection` type, cached on repeated access, `Dispose` doesn't throw), against an in-memory `IConfiguration`. No real SQL Server connection is opened.
- Sample: `dotnet run --project samples/Components.SQLServerRepository.Sample` — real DI wiring, builds `IConfiguration` from `appsettings.json` (+ environment variable override), and attempts a live `SELECT 1` round trip. Prints the connection string in use (password redacted) and a friendly explanation if the round trip fails, instead of a raw stack trace.

See the [components root README](../../README.md) for build/pack/test commands that apply to every package in this solution.
