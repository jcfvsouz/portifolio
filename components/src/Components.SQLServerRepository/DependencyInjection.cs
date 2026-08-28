using Components.SQLRepository;
using Microsoft.Extensions.DependencyInjection;

namespace Components.SQLServerRepository;

public static class DependencyInjection
{
    public static IServiceCollection AddSqlServerRepository(this IServiceCollection services)
    {
        services.AddScoped<ISqlRepository, SqlServerRepository>();
        return services;
    }
}
