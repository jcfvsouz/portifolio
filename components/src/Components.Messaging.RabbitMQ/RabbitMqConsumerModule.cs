using Components.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Components.Messaging.RabbitMQ;

public class RabbitMqConsumerModule<TEvent, THandler> : IStartup
    where TEvent : class
    where THandler : class, IEventHandler<TEvent>
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddRabbitMqConsumer<TEvent, THandler>(configuration);
}
