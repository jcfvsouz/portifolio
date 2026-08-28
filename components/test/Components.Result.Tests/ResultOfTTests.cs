using Components.Result;
using Components.TestSupport.Builders;

namespace Components.Result.Tests;

public class ResultOfTTests
{
    [Fact]
    public void Ok_returns_successful_result_carrying_content()
    {
        // Arrange
        const string content = "payload";

        // Act
        var result = Result<string>.Ok(content);

        // Assert
        result.Success.Should().BeTrue();
        result.Content.Should().Be(content);
    }

    [Fact]
    public void Error_with_message_returns_unsuccessful_result_with_no_content()
    {
        // Arrange
        const string errorMessage = "failed";

        // Act
        var result = Result<string>.Error(errorMessage);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be(errorMessage);
        result.Content.Should().BeNull();
    }

    [Fact]
    public void Error_with_validation_failures_joins_messages_with_pipe_and_has_no_content()
    {
        // Arrange
        var validation = new ValidationResultBuilder()
            .WithError("Name", "Name is required")
            .Build();

        // Act
        var result = Result<string>.Error(validation.Errors);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Name is required");
        result.Content.Should().BeNull();
    }
}
