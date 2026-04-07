namespace DistributedConfigHub.Client;

public class DistributedConfigOptions
{
    public string ApplicationName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string FallbackFilePath { get; set; } = "local-fallback-config.json";
    public string ApiKey { get; set; } = string.Empty;
    public string RabbitMqHostName { get; set; } = "localhost";
    public int RabbitMqPort { get; set; } = 5672;
    public string RabbitMqUserName { get; set; } = "guest";
    public string RabbitMqPassword { get; set; } = "guest";
    public string RabbitMqExchangeName { get; set; } = "config_updates_direct";
}
