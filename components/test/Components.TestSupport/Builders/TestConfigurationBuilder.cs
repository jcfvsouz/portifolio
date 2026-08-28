using Microsoft.Extensions.Configuration;

namespace Components.TestSupport.Builders;

/// <summary>
/// Fluent builder for an in-memory <see cref="IConfiguration"/>, so repository tests can supply
/// a connection string (or any other setting) without an appsettings.json file on disk.
/// </summary>
public class TestConfigurationBuilder
{
    private readonly Dictionary<string, string?> _values = [];

    public TestConfigurationBuilder WithConnectionString(string name, string value)
    {
        _values[$"ConnectionStrings:{name}"] = value;
        return this;
    }

    public TestConfigurationBuilder With(string key, string value)
    {
        _values[key] = value;
        return this;
    }

    public IConfiguration Build() =>
        new ConfigurationBuilder().AddInMemoryCollection(_values).Build();
}
