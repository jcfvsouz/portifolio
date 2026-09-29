using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Components.Hosting;

public interface IStartup
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
}
