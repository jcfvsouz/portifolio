using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Promo.Api.ExceptionHandling;
using IStartup = Components.Hosting.IStartup;

namespace Promo.Api.Configuration;

public class ExceptionHandlingModule : IStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
    }
}
