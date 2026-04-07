using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DistributedConfigHub.Client;

public class RabbitMqSubscriberHostedService : BackgroundService
{
    private readonly DistributedConfigOptions _options;
    private readonly IConfigSdkService _configSdkService;
    private readonly ILogger<RabbitMqSubscriberHostedService> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private string? _queueName;

    public RabbitMqSubscriberHostedService(DistributedConfigOptions options, IConfigSdkService configSdkService, ILogger<RabbitMqSubscriberHostedService> logger)
    {
        _options = options;
        _configSdkService = configSdkService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _configSdkService.ReloadConfigurationsAsync(stoppingToken);

        var factory = new ConnectionFactory
        {
            HostName = _options.RabbitMqHostName,
            Port = _options.RabbitMqPort,
            UserName = _options.RabbitMqUserName,
            Password = _options.RabbitMqPassword
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
                    _logger.LogInformation("RabbitMQ bağlantısı başarıyla kuruldu!");
                    break; // Bağlantı başarılı ise döngüden çık
                }
                catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException)
                {
                    _logger.LogWarning("RabbitMQ sunucusuna bağlanılamadı. {Delay} ms sonra tekrar denenecek...", delayMs);
                    await Task.Delay(delayMs, stoppingToken);
                    
                    // Bekleme süresini ikiye katla, ama kalabalık yapmaması için maksimum 60 saniyede bir dene
                    delayMs = Math.Min(delayMs * 2, 60000); 
                }
            }

            if (_connection == null) return;

            _channel = await _connection.CreateChannelAsync(options: null, cancellationToken: stoppingToken);

            await _channel.ExchangeDeclareAsync(_options.RabbitMqExchangeName, ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
            
            var queueDeclareResult = await _channel.QueueDeclareAsync(queue: string.Empty, durable: false, exclusive: true, autoDelete: true, arguments: null, cancellationToken: stoppingToken);
            _queueName = queueDeclareResult.QueueName;

            await _channel.QueueBindAsync(queue: _queueName, exchange: _options.RabbitMqExchangeName, routingKey: _options.ApplicationName, arguments: null, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                _logger.LogInformation("Received live update event from config hub: {Message}. Reloading configs...", message);
                await _configSdkService.ReloadConfigurationsAsync(stoppingToken);

                // Kullanıcının (İBB Müşterisi) isteği: Değer güncellendiğinde en güncel değerleri logla!
                var newPaymentGw = _configSdkService.GetString("PaymentGatewayUrl");
                var newLimit = _configSdkService.GetInt("MaxIstanbulKartTransactionsPerMin");
                _logger.LogInformation($"[EVENT BAŞARILI] Bellek güncellendi. Yeni Değerler -> Gateway: {newPaymentGw}, Limit: {newLimit}");
            };

            await _channel.BasicConsumeAsync(queue: _queueName, autoAck: true, consumer: consumer, cancellationToken: stoppingToken);
            _logger.LogInformation("RabbitMQ Background Subscriber initialized and listening to routing key: {RoutingKey}", _options.ApplicationName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while initializing RabbitMQ subscriber.");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken: cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken: cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
