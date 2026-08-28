using Components.TestSupport.Builders;
using Microsoft.Extensions.Configuration;

namespace Components.TestSupport.Fixtures;

/// <summary>
/// xUnit class fixture providing a ready-made <see cref="IConfiguration"/> with a SQL Server
/// connection string, built once per test class via <see cref="TestConfigurationBuilder"/>.
/// No real database is involved — these tests only exercise connection-string wiring and
/// object lifetime, never an actual open connection.
/// </summary>
public class SqlServerConfigurationFixture
{
    public const string ConnectionString =
        "Server=localhost;Database=CampaignPipelineTests;User Id=sa;Password=Test#12345;TrustServerCertificate=True;";

    public IConfiguration Configuration { get; } = new TestConfigurationBuilder()
        .WithConnectionString("SqlServer", ConnectionString)
        .Build();
}
