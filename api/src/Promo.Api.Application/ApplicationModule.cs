using Components.Hosting;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Promo.Api.Application.Requests;
using Promo.Api.Application.UseCases;
using Promo.Api.Application.Validators;
using Promo.Api.Domain.Services;

namespace Promo.Api.Application;

public class ApplicationModule : IStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IValidator<CreateCampaignRequest>, CreateCampaignRequestValidator>();
        services.AddScoped<IPublishCampaignService, PublishCampaignService>();
        services.AddScoped<CreateCampaignUseCase>();
        services.AddScoped<PublishCampaignUseCase>();
    }
}
