using Components.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.WriteLine("--- Components.Hosting sample ---");
Console.WriteLine("UseStartup<T> concentrates a module's DI registration into one named class");
Console.WriteLine("(mirrors classic ASP.NET Core's Startup.ConfigureServices), instead of spreading");
Console.WriteLine("services.AddXxx(...) calls across Program.cs.");
Console.WriteLine();

var builder = Host.CreateApplicationBuilder(args);
builder.UseStartup<GreetingModule>();

using var host = builder.Build();

var greeter = host.Services.GetRequiredService<IGreeter>();
Console.WriteLine(greeter.Greet());

internal interface IGreeter
{
    string Greet();
}

internal class Greeter(IConfiguration configuration) : IGreeter
{
    public string Greet() => $"Hello from {nameof(Greeter)}, registered via {nameof(GreetingModule)}.UseStartup — environment: {configuration["DOTNET_ENVIRONMENT"] ?? "(not set)"}.";
}

internal class GreetingModule : IStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddSingleton<IGreeter, Greeter>();
}
