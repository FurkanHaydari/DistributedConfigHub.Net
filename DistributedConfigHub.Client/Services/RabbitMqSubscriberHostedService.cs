using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DistributedConfigHub.Client.Interfaces;
using DistributedConfigHub.Client.Models;

namespace DistributedConfigHub.Client.Services;

public class RabbitMqSubscriberHostedService(
    DistributedConfigOptions options, 
    IConfigSdkService configSdkService, 
    ILogger<RabbitMqSubscriberHostedService> logger) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;
    private string? _queueName;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await configSdkService.ReloadConfigurationsAsync(stoppingToken);

        var factory = new ConnectionFactory
        {
            HostName = options.RabbitMqHostName,
            Port = options.RabbitMqPort,
            UserName = options.RabbitMqUserName,
            Password = options.RabbitMqPassword
        };

        try
        {
            // RabbitMQ container'ı Postgres'e göre daha geç ayağa kalktığı için 
            // "Exponential Backoff" (Kademeli Bekleme) döngüsü kullanıyoruz.
            int delayMs = 5000;
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _connection = await factory.CreateConnectionAsync(stoppingToken);
                    logger.LogInformation("RabbitMQ bağlantısı başarıyla kuruldu!");
                    break; // Bağlantı başarılı ise döngüden çık
                }
                catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException)
                {
                    logger.LogWarning("RabbitMQ sunucusuna bağlanılamadı. {Delay} ms sonra tekrar denenecek...", delayMs);
                    await Task.Delay(delayMs, stoppingToken);
                    
                    // Bekleme süresini ikiye katla, ama kalabalık yapmaması için maksimum 60 saniyede bir dene
                    delayMs = Math.Min(delayMs * 2, 60000); 
                }
            }

            if (_connection == null) return;

            _channel = await _connection.CreateChannelAsync(options: null, cancellationToken: stoppingToken);

            await _channel.ExchangeDeclareAsync(options.RabbitMqExchangeName, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
            
            var queueDeclareResult = await _channel.QueueDeclareAsync(queue: string.Empty, durable: false, exclusive: true, autoDelete: true, arguments: null, cancellationToken: stoppingToken);
            _queueName = queueDeclareResult.QueueName;

            await _channel.QueueBindAsync(queue: _queueName, exchange: options.RabbitMqExchangeName, routingKey: options.ApplicationName, arguments: null, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                // Mesaj formatı: "SERVICE-A|dev" — environment kısmını parse et
                var parts = message.Split('|');
                if (parts.Length == 2)
                {
                    var incomingEnvironment = parts[1];
                    if (!string.Equals(incomingEnvironment, options.Environment, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogInformation("Ignoring config update for environment '{IncomingEnv}' (current: '{CurrentEnv}'). Message: {Message}", 
                            incomingEnvironment, options.Environment, message);
                        return;
                    }
                }

                logger.LogInformation("Received config update for current environment '{Environment}': {Message}. Reloading configs...", 
                    options.Environment, message);
                await configSdkService.ReloadConfigurationsAsync(stoppingToken);

                // Kullanıcı tarafından tanımlanan callback'i çağır (varsa)
                if (options.OnConfigurationUpdated != null)
                {
                    await options.OnConfigurationUpdated.Invoke(configSdkService);
                }
            };

            await _channel.BasicConsumeAsync(queue: _queueName, autoAck: true, consumer: consumer, cancellationToken: stoppingToken);
            logger.LogInformation("RabbitMQ Background Subscriber initialized and listening to routing key: {RoutingKey}", options.ApplicationName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while initializing RabbitMQ subscriber.");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken: cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken: cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
