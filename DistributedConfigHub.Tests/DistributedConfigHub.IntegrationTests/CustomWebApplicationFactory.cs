using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using DistributedConfigHub.Infrastructure.Data;
using DistributedConfigHub.Infrastructure.Data.Interceptors;
using Xunit;
using Microsoft.AspNetCore.TestHost;

namespace DistributedConfigHub.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;

    public System.Text.Json.JsonSerializerOptions DefaultJsonOptions { get; } = new System.Text.Json.JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public HttpClient CreateAuthenticatedClient(string apiKey = "service-a-secret-key")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        return client;
    }

    public CustomWebApplicationFactory()
    {
        _dbContainer = new PostgreSqlBuilder("postgres:15-alpine")
            .WithDatabase("ConfigHubDb_Test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:3-management-alpine")
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // We remove the original database setting (in Program.cs) to prevent leakage
            services.RemoveAll<DbContextOptions<ConfigDbContext>>();

            // Clean DB setting to run isolated over Testcontainers
            services.AddSingleton<AuditInterceptor>();
            services.AddDbContext<ConfigDbContext>((sp, options) =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
                options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ConfigDbContext>();
            // Migration and seed data only run over this container DB.
            db.Database.Migrate();
        });

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var confDict = new Dictionary<string, string>
            {
                { "RabbitMQ:HostName", _rabbitMqContainer.Hostname },
                { "RabbitMQ:Port", _rabbitMqContainer.GetMappedPublicPort(5672).ToString() },
                { "RabbitMQ:UserName", "guest" },
                { "RabbitMQ:Password", "guest" }
            };
            config.AddInMemoryCollection(confDict!);
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
    }
}
