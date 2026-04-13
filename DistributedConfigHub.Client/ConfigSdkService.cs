using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DistributedConfigHub.Client;

public class ConfigSdkService(HttpClient httpClient, DistributedConfigOptions options, ILogger<ConfigSdkService> logger) : IConfigSdkService
{
    // volatile: Referans değişimi anında tüm thread'ler tarafından görülür (atomic swap)
    private volatile ConcurrentDictionary<string, ConfigurationItem> _cache = new();
    // Aynı anda sadece 1 thread'in işlem yapmasını sağlayan kilit mekanizması
    private readonly SemaphoreSlim _reloadLock = new(1, 1);
    
    public string? GetString(string key) => _cache.TryGetValue(key, out var item) ? item.Value : null;

    public int GetInt(string key, int defaultValue = 0)
    {
        if (_cache.TryGetValue(key, out var item) && int.TryParse(item.Value, out var parsed))
            return parsed;
        return defaultValue;
    }

    public double GetDouble(string key, double defaultValue = 0.0)
    {
        if (_cache.TryGetValue(key, out var item) && double.TryParse(item.Value, out var parsed))
            return parsed;
        return defaultValue;
    }

    public bool GetBoolean(string key, bool defaultValue = false)
    {
        if (_cache.TryGetValue(key, out var item))
        {
            if (bool.TryParse(item.Value, out var parsed))
                return parsed;
            
            if (item.Value == "1") return true;
            if (item.Value == "0") return false;
        }
        return defaultValue;
    }

    public IReadOnlyDictionary<string, string> GetAll()
    {
        return _cache.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value);
    }
    

    public async Task ReloadConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        // Eğer kilitliyse, asenkron olarak kilidin açılmasını bekle
        await _reloadLock.WaitAsync(cancellationToken);
        try
        {
            var url = $"Configurations?applicationName={options.ApplicationName}&environment={options.Environment}";
            var response = await httpClient.GetAsync(url, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var items = JsonSerializer.Deserialize<List<ConfigurationItem>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (items is not null)
                {
                    SwapCache(items);
                    
                    await File.WriteAllTextAsync(options.FallbackFilePath, content, cancellationToken);
                    logger.LogInformation("Configurations successfully loaded from API and cached to {FallbackFile}.", options.FallbackFilePath);
                    return;
                }
            }
            
            logger.LogWarning("API responded with {StatusCode}. Attempting to read from local fallback.", response.StatusCode);
            await ReadFromFallbackAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch configurations from API. Attempting to use local fallback.");
            await ReadFromFallbackAsync(cancellationToken);
        }
        finally
        {
            // Kiliti serbest bırak
            _reloadLock.Release();
        }
    }

    private async Task ReadFromFallbackAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(options.FallbackFilePath))
        {
            var content = await File.ReadAllTextAsync(options.FallbackFilePath, cancellationToken);
            var items = JsonSerializer.Deserialize<List<ConfigurationItem>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (items is not null)
            {
                SwapCache(items);
                logger.LogInformation("Configurations loaded from local fallback file.");
            }
        }
        else
        {
            logger.LogWarning("Local fallback file not found at {FallbackFile}. The configuration cache remains empty.", options.FallbackFilePath);
        }
    }

    /// <summary>
    /// Yeni bir dictionary oluşturup referansı atomik olarak değiştirir.
    /// Bu sayede okuyucu thread'ler asla boş veya yarım yüklenmiş bir cache görmez.
    /// </summary>
    private void SwapCache(List<ConfigurationItem> items)
    {
        var newCache = new ConcurrentDictionary<string, ConfigurationItem>();
        foreach (var item in items)
        {
            newCache[item.Name] = item;
        }
        _cache = newCache; // Atomic reference swap (volatile)
    }
}
