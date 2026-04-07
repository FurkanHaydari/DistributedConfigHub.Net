namespace DistributedConfigHub.Client;

public interface IConfigSdkService
{
    string? GetString(string key);
    int GetInt(string key, int defaultValue = 0);
    double GetDouble(string key, double defaultValue = 0.0);
    bool GetBoolean(string key, bool defaultValue = false);
    Task ReloadConfigurationsAsync(CancellationToken cancellationToken = default);
}
