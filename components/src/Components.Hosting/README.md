# Components.Hosting

`IStartup` and `UseStartup<TStartup>()` — a composition-root convention (mirrors classic ASP.NET Core's `Startup` class) for concentrating a module's DI registration into one named, discoverable class, instead of spreading `services.AddXxx(...)` calls across `Program.cs`.

```csharp
public interface IStartup
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
}

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder UseStartup<TStartup>(this IHostApplicationBuilder builder)
        where TStartup : IStartup, new();
    // constructs TStartup and calls ConfigureServices(builder.Services, builder.Configuration)
}
```

## Why

As an app grows, `Program.cs` tends to accumulate a long, unstructured list of `services.AddXxx(...)` calls for every module it wires up (messaging, persistence, background jobs, ...). Naming each module's registration as its own `IStartup` class gives it a place to live, a name that shows up in "go to definition," and a natural unit to point a code reviewer at ("what does this module register?" → open its `Startup` class) — the same reason classic ASP.NET Core kept `Startup.ConfigureServices` as a dedicated method instead of inlining everything into `Main`.

This is a build-time/composition concern, unrelated to runtime background-service lifecycles — a module that also needs a long-running background process (like [`Components.Messaging`](../Components.Messaging/README.md)'s `EventConsumer<TEvent>`) still uses `services.AddHostedService<T>()` inside its `IStartup.ConfigureServices` like anything else; `Components.Hosting` doesn't get involved in what happens after the host starts, only in how it's wired up before that.

## Usage

```csharp
public class GreetingModule : IStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddSingleton<IGreeter, Greeter>();
}

var builder = Host.CreateApplicationBuilder(args);
builder.UseStartup<GreetingModule>(); // chainable: .UseStartup<A>().UseStartup<B>()...
```

[`Components.Messaging.RabbitMQ`](../Components.Messaging.RabbitMQ/README.md)'s `RabbitMqPublisherModule` and `RabbitMqConsumerModule<TEvent, THandler>` are real `IStartup` implementations built on this.

## Tests and sample

- Tests: `test/Components.Hosting.Tests` — `ConfigureServices` runs exactly once per `UseStartup<T>()` call, receives the builder's own `IServiceCollection`/`IConfiguration` instances (not copies), registrations made inside it are resolvable once the host is built, and the extension returns the same builder instance so calls can be chained.
- Sample: `dotnet run --project samples/Components.Hosting.Sample` — a `GreetingModule : IStartup` registering a trivial `IGreeter`, wired via `UseStartup<GreetingModule>()` into a real `Microsoft.Extensions.Hosting` generic host, then resolved and used after `Build()`.

See the [components root README](../../README.md) for build/pack/test commands that apply to every package in this solution.
