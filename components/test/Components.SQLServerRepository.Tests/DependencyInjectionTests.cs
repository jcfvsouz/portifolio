using Components.SQLRepository;
using Components.TestSupport.Fixtures;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace Components.SQLServerRepository.Tests;

public class DependencyInjectionTests(SqlServerConfigurationFixture fixture) : IClassFixture<SqlServerConfigurationFixture>
{
    [Fact]
    public void AddSqlServerRepository_registers_ISqlRepository_as_scoped_SqlServerRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSqlServerRepository();

        // Assert
        var descriptor = services.Should().ContainSingle(d => d.ServiceType == typeof(ISqlRepository)).Which;
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
        descriptor.ImplementationType.Should().Be(typeof(SqlServerRepository));
    }

    [Fact]
    public void AddSqlServerRepository_resolves_to_a_working_repository()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(fixture.Configuration);
        services.AddSqlServerRepository();

        // Act
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISqlRepository>();

        // Assert
        repository.Should().BeOfType<SqlServerRepository>();
        repository.Connection.Should().BeOfType<SqlConnection>();
    }
}
