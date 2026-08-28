using Components.TestSupport.Builders;
using FluentValidation.Results;

namespace Components.TestSupport.Fixtures;

/// <summary>
/// xUnit class fixture (<see cref="Xunit.IClassFixture{T}"/>): builds the valid/invalid
/// <see cref="ValidationResult"/> instances once per test class and shares them across every
/// [Fact] in that class, instead of re-building the same data in each test method.
/// </summary>
public class ValidationResultFixture
{
    public ValidationResult Valid { get; } = ValidationResultBuilder.Valid();

    public ValidationResult Invalid { get; } = new ValidationResultBuilder()
        .WithError("Name", "Name is required")
        .WithError("Email", "Email is invalid")
        .Build();
}
