namespace DistributedConfigHub.Client.Interfaces;

public interface IConfigSdkService
{
    string? GetString(string key);
    int GetInt(string key, int defaultValue = 0);
    double GetDouble(string key, double defaultValue = 0.0);
    bool GetBoolean(string key, bool defaultValue = false);
    
    /// <summary>
    /// Returns all configurations in memory (Key -> Value).
    /// </summary>
    IReadOnlyDictionary<string, string> GetAll();
    
    Task ReloadConfigurationsAsync(CancellationToken cancellationToken = default);
}
