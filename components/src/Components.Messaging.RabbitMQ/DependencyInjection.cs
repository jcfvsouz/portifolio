using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Components.Messaging.RabbitMQ;

public static class DependencyInjection
{
    public static IServiceCollection AddRabbitMqPublisher(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));

        services.AddSingleton<IConnection>(serviceProvider =>
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

        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        return services;
    }
}
