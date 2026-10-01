using Components.Hosting;
using Components.Messaging.RabbitMQ;
using Components.SQLServerRepository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Promo.Api.Application.Notifications;
using Promo.Api.Domain.Repositories;
using Promo.Api.Infrastructure.Notifications;
using Promo.Api.Infrastructure.Repositories;

namespace Promo.Api.Infrastructure;

public class InfrastructureModule : IStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSqlServerRepository();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IBuyerGroupRepository, BuyerGroupRepository>();
        services.AddScoped<ICampaignPublishedNotifier, CampaignPublishedNotification>();

        new RabbitMqPublisherModule().ConfigureServices(services, configuration);
    }
}
