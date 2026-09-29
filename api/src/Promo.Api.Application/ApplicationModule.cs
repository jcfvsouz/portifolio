using Components.Hosting;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Promo.Api.Application;

public class ApplicationModule : IStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IValidator<CreateCampaignRequest>, CreateCampaignRequestValidator>();
        services.AddScoped<CreateCampaignUseCase>();
        services.AddScoped<PublishCampaignUseCase>();
    }
}
