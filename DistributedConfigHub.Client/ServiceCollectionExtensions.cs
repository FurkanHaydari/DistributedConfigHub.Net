using Microsoft.Extensions.DependencyInjection;

namespace DistributedConfigHub.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDistributedConfigHub(this IServiceCollection services, Action<DistributedConfigOptions> configureOptions)
    {
        var options = new DistributedConfigOptions();
        configureOptions(options);

        services.AddSingleton(options);
        
        services.AddHttpClient("ConfigHub", client =>
        {
            client.BaseAddress = new Uri(options.ApiBaseUrl.EndsWith('/') ? options.ApiBaseUrl : options.ApiBaseUrl + "/");
            client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
        })
        .SetHandlerLifetime(TimeSpan.FromMinutes(5));
        
        services.AddSingleton<IConfigSdkService>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient("ConfigHub");
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ConfigSdkService>>();
            return new ConfigSdkService(httpClient, options, logger);
        });
        
        services.AddHostedService<RabbitMqSubscriberHostedService>();

        return services;
    }
}

