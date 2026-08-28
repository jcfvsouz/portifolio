using FluentValidation.Results;

namespace Components.Result;

public class Result
{
    public string ErrorMessage { get; set; } = string.Empty;
    public bool Success { get; set; }

    public static Result Ok() => new() { Success = true };
    public static Result Error(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
    public static Result Error(List<ValidationFailure> errors) => new()
    {
        Success = false,
        ErrorMessage = string.Join("|", errors.Select(err => err.ErrorMessage)),
    };
}

public class Result<T> : Result
{
    public T? Content { get; set; }

    public static Result<T> Ok(T content) => new() { Success = true, Content = content };
    public static new Result<T> Error(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
    public static new Result<T> Error(List<ValidationFailure> errors) => new()
    {
        Success = false,
        ErrorMessage = string.Join("|", errors.Select(err => err.ErrorMessage)),
    };
}
