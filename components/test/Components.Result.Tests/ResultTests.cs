using Components.Result;
using Components.TestSupport.Builders;

namespace Components.Result.Tests;

public class ResultTests
{
    [Fact]
    public void Ok_returns_successful_result_with_no_error_message()
    {
        // Act
        var result = Result.Ok();

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public void Error_with_message_returns_unsuccessful_result_carrying_that_message()
    {
        // Arrange
        const string errorMessage = "something went wrong";

        // Act
        var result = Result.Error(errorMessage);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be(errorMessage);
    }

    [Fact]
    public void Error_with_validation_failures_joins_messages_with_pipe()
    {
        // Arrange
        var validation = new ValidationResultBuilder()
            .WithError("Name", "Name is required")
            .WithError("Email", "Email is invalid")
            .Build();

        // Act
        var result = Result.Error(validation.Errors);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Name is required|Email is invalid");
    }
}
