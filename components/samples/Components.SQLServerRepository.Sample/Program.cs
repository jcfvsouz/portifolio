using Components.SQLRepository;
using Components.SQLServerRepository;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("--- Components.SQLServerRepository sample ---");
Console.WriteLine();

// appsettings.json ships a default connection string for the SQL Server container this
// project's docker-compose will bring up (see ../../../docker/, once it exists). Override it
// without editing the file by setting the environment variable ConnectionStrings__SqlServer.
IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("SqlServer")!;
Console.WriteLine($"Connection string in use: {Redact(connectionString)}");
Console.WriteLine();

var services = new ServiceCollection();
services.AddSingleton(configuration);
services.AddSqlServerRepository();

using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
using var repository = scope.ServiceProvider.GetRequiredService<ISqlRepository>();

Console.WriteLine($"Resolved ISqlRepository -> {repository.GetType().Name}");
Console.WriteLine($"Connection type -> {repository.Connection.GetType().Name}");
Console.WriteLine();

Console.WriteLine("Attempting a real round trip: SELECT 1 ...");
try
{
    var result = repository.Connection.QuerySingle<int>("SELECT 1");
    Console.WriteLine($"Success — SQL Server responded with: {result}");
}
catch (Exception ex)
{
    Console.WriteLine("Could not complete the round trip — either no SQL Server is listening on");
    Console.WriteLine("localhost:1433 yet (the container from ../../../docker/ isn't up), or the");
    Console.WriteLine("credentials in appsettings.json don't match a server that's already running.");
    Console.WriteLine("Either way, the wiring above (DI, connection string, Dapper) is what this");
    Console.WriteLine("sample exists to prove works — adjust appsettings.json and re-run.");
    Console.WriteLine($"Underlying error: {ex.GetType().Name}: {ex.Message}");
}

static string Redact(string connectionString) =>
    System.Text.RegularExpressions.Regex.Replace(connectionString, "Password=[^;]*", "Password=***");
