using Microsoft.Extensions.DependencyInjection;

namespace DistributedConfigHub.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDistributedConfigHub(this IServiceCollection services, Action<DistributedConfigOptions> configureOptions)
    {
        var options = new DistributedConfigOptions();
        configureOptions(options);

        services.AddSingleton(options);
        
        // Named HttpClient + IHttpClientFactory kaydı (DNS havuz yönetimi için)
        services.AddHttpClient("ConfigHub")
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));
        
        // ConfigSdkService Singleton olarak kaydedilmeli çünkü:
        // 1. BackgroundService (RabbitMqSubscriberHostedService) Singleton'dır
        // 2. ConcurrentDictionary cache'i tüm uygulama yaşam döngüsünce korunmalıdır
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

