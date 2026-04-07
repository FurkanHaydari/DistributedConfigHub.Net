using System.Text;
using DistributedConfigHub.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace DistributedConfigHub.Infrastructure.Messaging;

public class RabbitMqPublisher(IConfiguration configuration) : IMessagePublisher
{
    public async Task PublishConfigurationUpdatedEventAsync(string applicationName, string environment, CancellationToken cancellationToken = default)
    {
        var hostname = configuration["RabbitMQ:HostName"] ?? "localhost";
        var port = int.TryParse(configuration["RabbitMQ:Port"], out var parsedPort) ? parsedPort : 5672;
        var userName = configuration["RabbitMQ:UserName"] ?? "guest";
        var password = configuration["RabbitMQ:Password"] ?? "guest";
        var exchangeName = configuration["RabbitMQ:ExchangeName"] ?? "config_updates_direct";

        var factory = new ConnectionFactory
        {
            HostName = hostname,
            Port = port,
            UserName = userName,
            Password = password
        };

        using var connection = await factory.CreateConnectionAsync(cancellationToken);
        using var channel = await connection.CreateChannelAsync(options: null, cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null, cancellationToken: cancellationToken);

        var routingKey = applicationName;
        var message = $"{applicationName}|{environment}";
        var body = Encoding.UTF8.GetBytes(message);

        // Note: Newest RabbitMQ.Client uses BasicPublishAsync taking BasicProperties instead of props inline optionally
        await channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: routingKey,
            body: body,
            cancellationToken: cancellationToken);
    }
}
