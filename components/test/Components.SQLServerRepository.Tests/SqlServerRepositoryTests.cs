using Components.TestSupport.Fixtures;
using Microsoft.Data.SqlClient;

namespace Components.SQLServerRepository.Tests;

public class SqlServerRepositoryTests(SqlServerConfigurationFixture fixture) : IClassFixture<SqlServerConfigurationFixture>
{
    [Fact]
    public void Connection_is_a_sql_connection_using_the_configured_connection_string()
    {
        // Arrange
        using var repository = new SqlServerRepository(fixture.Configuration);

        // Act
        var connection = repository.Connection;

        // Assert
        connection.Should().BeOfType<SqlConnection>();
        connection.ConnectionString.Should().Be(SqlServerConfigurationFixture.ConnectionString);
    }

    [Fact]
    public void Connection_returns_the_same_instance_on_repeated_access()
    {
        // Arrange
        using var repository = new SqlServerRepository(fixture.Configuration);

        // Act
        var first = repository.Connection;
        var second = repository.Connection;

        // Assert
        second.Should().BeSameAs(first);
    }

    [Fact]
    public void Dispose_does_not_throw_after_the_connection_was_created_but_never_opened()
    {
        // Arrange
        var repository = new SqlServerRepository(fixture.Configuration);
        _ = repository.Connection;

        // Act
        var act = repository.Dispose;

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_does_not_throw_when_connection_was_never_accessed()
    {
        // Arrange
        var repository = new SqlServerRepository(fixture.Configuration);

        // Act
        var act = repository.Dispose;

        // Assert
        act.Should().NotThrow();
    }
}
