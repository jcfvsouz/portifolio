# Components.Result

`Result` and `Result<T>` — a wrapper type that makes a use case's outcome explicit instead of relying on exceptions for expected, business-rule failures.

## Why

The convention across this codebase is that **business-rule failures are never exceptions**. A use case that rejects an empty campaign name isn't in an exceptional state — it's a normal outcome the caller (a controller, a worker handler) needs to branch on. Throwing for that forces every caller into try/catch just to read a message, and conflates "expected rejection" with "something is actually broken." Exceptions stay reserved for the second case: a dropped DB connection, a bug, anything the caller can't reasonably be expected to handle inline.

## API

```csharp
public class Result
{
    public bool Success { get; }
    public string ErrorMessage { get; }

    public static Result Ok();
    public static Result Error(string errorMessage);
    public static Result Error(List<ValidationFailure> errors); // joins messages with "|"
}

public class Result<T> : Result
{
    public T? Content { get; }

    public static Result<T> Ok(T content);
    public static new Result<T> Error(string errorMessage);
    public static new Result<T> Error(List<ValidationFailure> errors);
}
```

`Result<T>` inherits `Result`, so a method that only needs a yes/no outcome returns `Result`, and one that needs to hand back data on success returns `Result<T>` — both expose the same `Success`/`ErrorMessage` shape to the caller.

## Usage

```csharp
public Result<Campaign> Publish(PublishCampaignRequest request)
{
    if (request.BuyerGroupId is null)
        return Result<Campaign>.Error("A campaign must target a buyer group.");

    var campaign = new Campaign(request);
    return Result<Campaign>.Ok(campaign);
}

var result = Publish(request);
if (!result.Success)
    return BadRequest(result.ErrorMessage);
```

The `List<ValidationFailure>` overload exists so this type composes directly with FluentValidation results — see [`Extensions.FluentResult`](../Extensions.FluentResult/README.md), which turns a `ValidationResult` into a `Result`/`Result<T>` in one call instead of hand-rolling the mapping at every call site.

## Tests and sample

- Tests: `test/Components.Result.Tests` — every factory method, on both `Result` and `Result<T>`.
- Sample: `dotnet run --project samples/Components.Result.Sample` — calls every factory method and prints the resulting `Success`/`ErrorMessage`/`Content`.

See the [components root README](../../README.md) for build/pack/test commands that apply to every package in this solution.
