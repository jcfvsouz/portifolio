using Components.Hosting;
using Components.Messaging;
using Components.Messaging.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.WriteLine("--- Components.Messaging.RabbitMQ sample ---");
Console.WriteLine();

// appsettings.json ships the default credentials of the RabbitMQ container this project's
// docker-compose will bring up (see ../../../docker/, once it exists). Override it without
// editing the file by setting environment variables like RabbitMq__Host.
var builder = Host.CreateApplicationBuilder(args);
builder.UseStartup<RabbitMqPublisherModule>();

using var host = builder.Build();

Console.WriteLine($"Broker in use: {builder.Configuration["RabbitMq:Host"]}:{builder.Configuration["RabbitMq:Port"]}");
Console.WriteLine($"Exchange in use: {builder.Configuration["RabbitMq:ExchangeName"]}");
Console.WriteLine();

Console.WriteLine("Attempting a real publish: SampleCampaignPublished ...");
try
{
    var publisher = host.Services.GetRequiredService<IEventPublisher>();
    Console.WriteLine($"Resolved IEventPublisher -> {publisher.GetType().Name}");

    await publisher.PublishAsync(new SampleCampaignPublished("Black Friday 2026", BuyerCount: 4200));
    Console.WriteLine("Success — message published. Check the RabbitMQ management UI");
    Console.WriteLine("(http://localhost:15672, guest/guest) under the configured exchange.");
}
catch (Exception ex)
{
    Console.WriteLine("Could not complete the publish — either no RabbitMQ broker is listening on");
    Console.WriteLine("the configured host/port yet (the container from ../../../docker/ isn't up),");
    Console.WriteLine("or the credentials in appsettings.json don't match a broker that's already");
    Console.WriteLine("running. Either way, the wiring above (DI via UseStartup<RabbitMqPublisherModule>,");
    Console.WriteLine("options binding, channel/exchange setup) is what this sample exists to prove");
    Console.WriteLine("works — adjust appsettings.json and re-run.");
    Console.WriteLine($"Underlying error: {ex.GetType().Name}: {ex.Message}");
}

internal record SampleCampaignPublished(string CampaignName, int BuyerCount);
