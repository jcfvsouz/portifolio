# Extensions.FluentResult

Two extension methods, `ToResult()` and `ToResult<T>(content)`, that turn a FluentValidation `ValidationResult` directly into a [`Result` / `Result<T>`](../Components.Result/README.md).

## Why

Without this, every use case that validates input would hand-roll the same mapping:

```csharp
var validation = await _validator.ValidateAsync(dto);
if (!validation.IsValid)
    return Result<CampaignDto>.Error(validation.Errors);
```

Repeated across every use case, that `if` is pure noise — the intent is always "turn a validation outcome into a Result," never anything use-case-specific. Extracting it into an extension method makes the call site a single expression and keeps the mapping rule (join error messages with `|`, see `Components.Result`) defined in exactly one place.

## API

```csharp
public static Result ToResult(this ValidationResult validationResult);
public static Result<T> ToResult<T>(this ValidationResult validationResult, T content);
```

## Usage

```csharp
var validation = await _validator.ValidateAsync(dto);
if (!validation.IsValid)
    return validation.ToResult<CampaignDto>(dto);

return validation.ToResult(dto); // or, with no content payload: validation.ToResult()
```

## Tests and sample

- Tests: `test/Extensions.FluentResult.Tests` — a valid and an invalid `ValidationResult`, both extension methods.
- Sample: `dotnet run --project samples/Extensions.FluentResult.Sample` — defines a throwaway `CampaignDto` + validator, validates a valid and an invalid instance, and prints what `ToResult()`/`ToResult<T>()` produce for each.

See the [components root README](../../README.md) for build/pack/test commands that apply to every package in this solution.
