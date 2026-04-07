using Microsoft.Extensions.DependencyInjection;

namespace DistributedConfigHub.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDistributedConfigHub(this IServiceCollection services, Action<DistributedConfigOptions> configureOptions)
    {
        var options = new DistributedConfigOptions();
        configureOptions(options);

        services.AddSingleton(options);
        
        services.AddHttpClient<IConfigSdkService, ConfigSdkService>();
        
        services.AddHostedService<RabbitMqSubscriberHostedService>();

        return services;
    }
}
