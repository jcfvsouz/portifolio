using Microsoft.Extensions.Hosting;

namespace Components.Hosting;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder UseStartup<TStartup>(this IHostApplicationBuilder builder)
        where TStartup : IStartup, new()
    {
        new TStartup().ConfigureServices(builder.Services, builder.Configuration);
        return builder;
    }
}
