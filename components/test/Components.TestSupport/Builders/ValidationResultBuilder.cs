using FluentValidation.Results;

namespace Components.TestSupport.Builders;

/// <summary>
/// Fluent builder for a FluentValidation <see cref="ValidationResult"/>, so tests construct
/// valid/invalid outcomes by chaining <see cref="WithError"/> calls instead of hand-building
/// a <c>List&lt;ValidationFailure&gt;</c> inline in every test method.
/// </summary>
public class ValidationResultBuilder
{
    private readonly List<ValidationFailure> _failures = [];

    public ValidationResultBuilder WithError(string propertyName, string errorMessage)
    {
        _failures.Add(new ValidationFailure(propertyName, errorMessage));
        return this;
    }

    public ValidationResult Build() => new(_failures);

    public static ValidationResult Valid() => new();
}
