using Components.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Components.Messaging.RabbitMQ;

public class RabbitMqPublisherModule : IStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddRabbitMqPublisher(configuration);
}
