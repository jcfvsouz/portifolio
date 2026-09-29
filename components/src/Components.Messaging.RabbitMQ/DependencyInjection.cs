using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Components.Messaging.RabbitMQ;

public static class DependencyInjection
{
    public static IServiceCollection AddRabbitMqPublisher(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRabbitMqConnection(configuration);
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        return services;
    }

    public static IServiceCollection AddRabbitMqConsumer<TEvent, THandler>(this IServiceCollection services, IConfiguration configuration)
        where TEvent : class
        where THandler : class, IEventHandler<TEvent>
    {
        services.AddRabbitMqConnection(configuration);
        services.Configure<EventConsumerOptions>(typeof(TEvent).Name, configuration.GetSection($"Consumers:{typeof(TEvent).Name}"));
        services.AddScoped<IEventHandler<TEvent>, THandler>();
        services.AddSingleton<IEventConsumerStartup<TEvent>, RabbitMqConsumerStartup<TEvent>>();
        services.AddHostedService<EventConsumer<TEvent>>();

        return services;
    }

    private static IServiceCollection AddRabbitMqConnection(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));

        services.TryAddSingleton<IConnection>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
            var factory = new ConnectionFactory
            {
                HostName = options.Host,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password,
                VirtualHost = options.VirtualHost
            };

            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        });

        return services;
    }
}
