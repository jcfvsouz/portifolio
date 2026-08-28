using Components.TestSupport.Fixtures;
using Extensions.FluentResult;

namespace Extensions.FluentResult.Tests;

public class FluentResultExtensionsTests(ValidationResultFixture fixture) : IClassFixture<ValidationResultFixture>
{
    [Fact]
    public void ToResult_maps_a_valid_validation_result_to_ok()
    {
        // Arrange
        var validation = fixture.Valid;

        // Act
        var result = validation.ToResult();

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public void ToResult_maps_an_invalid_validation_result_to_error_with_joined_messages()
    {
        // Arrange
        var validation = fixture.Invalid;

        // Act
        var result = validation.ToResult();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Name is required|Email is invalid");
    }

    [Fact]
    public void ToResultOfT_maps_a_valid_validation_result_to_ok_carrying_the_given_content()
    {
        // Arrange
        var validation = fixture.Valid;
        const string content = "campaign-created";

        // Act
        var result = validation.ToResult(content);

        // Assert
        result.Success.Should().BeTrue();
        result.Content.Should().Be(content);
    }

    [Fact]
    public void ToResultOfT_maps_an_invalid_validation_result_to_error_with_no_content()
    {
        // Arrange
        var validation = fixture.Invalid;

        // Act
        var result = validation.ToResult("campaign-created");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Name is required|Email is invalid");
        result.Content.Should().BeNull();
    }
}
