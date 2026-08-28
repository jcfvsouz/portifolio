using FluentValidation.Results;

namespace Extensions.FluentResult;

public static class FluentResultExtensions
{
    /// <summary>Maps a validation outcome to Result.Ok() / Result.Error(errors), with no content payload.</summary>
    public static Components.Result.Result ToResult(this ValidationResult validationResult) =>
        validationResult.IsValid
            ? Components.Result.Result.Ok()
            : Components.Result.Result.Error(validationResult.Errors);

    /// <summary>Maps a validation outcome to Result&lt;T&gt;.Ok(content) when valid, or Result&lt;T&gt;.Error(errors) when not.</summary>
    public static Components.Result.Result<T> ToResult<T>(this ValidationResult validationResult, T content) =>
        validationResult.IsValid
            ? Components.Result.Result<T>.Ok(content)
            : Components.Result.Result<T>.Error(validationResult.Errors);
}
