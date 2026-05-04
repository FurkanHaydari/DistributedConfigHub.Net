using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using DistributedConfigHub.Client.Interfaces;
using DistributedConfigHub.Client.Models;

namespace DistributedConfigHub.Client.Services;

public class ConfigSdkService(HttpClient httpClient, DistributedConfigOptions options, ILogger<ConfigSdkService> logger) : IConfigSdkService
{
    // volatile: Reference change is seen by all threads instantly (atomic swap)
    private volatile ConcurrentDictionary<string, ConfigurationItem> _cache = new();
    // Locking mechanism ensuring only 1 thread processes at a time
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
        // If locked, wait asynchronously for the lock to be released
        await _reloadLock.WaitAsync(cancellationToken);
        try
        {
            var url = $"Configurations?applicationName={options.ApplicationName}&environment={options.Environment}";
            
            HttpResponseMessage? response = null;
            Exception? lastException = null;

            // 1. Retry Mechanism (3 attempts) - Kademeli bekleme ile
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    response = await httpClient.GetAsync(url, cancellationToken);
                    if (response.IsSuccessStatusCode) break;
                    
                    logger.LogWarning("API request failed (Attempt {Attempt}/3). Status: {StatusCode}", attempt, response.StatusCode);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    logger.LogWarning(ex, "API request threw exception (Attempt {Attempt}/3).", attempt);
                }

                if (attempt < 3) await Task.Delay(1000 * attempt, cancellationToken); 
            }

            // 2. Success Case - Check if valid data arrived
            if (response != null && response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var items = JsonSerializer.Deserialize<List<ConfigurationItem>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                // LKGC Guard: If empty data arrives and we already have data, preserve the Last Known Good Configuration
                if (items == null || items.Count == 0)
                {
                    if (!_cache.IsEmpty)
                    {
                        logger.LogWarning("API returned empty configurations. Keeping the last known good memory cache to prevent data loss.");
                        return;
                    }
                }
                else
                {
                    SwapCache(items);
                    
                    // Only update fallback file when successful and populated data arrives from API
                    await File.WriteAllTextAsync(options.FallbackFilePath, content, cancellationToken);
                    logger.LogInformation("Configurations successfully loaded from API and cached to {FallbackFile}.", options.FallbackFilePath);
                    return;
                }
            }
            
            // 3. Failure Case - If API fails, preserve data in memory or check fallback
            if (!_cache.IsEmpty)
            {
                logger.LogWarning("API fetch failed after retries. Keeping the existing memory cache to ensure service continuity. Last error: {Error}", 
                    lastException?.Message ?? response?.StatusCode.ToString());
            }
            else
            {
                logger.LogWarning("API fetch failed and memory cache is empty. Attempting to use local fallback file.");
                await ReadFromFallbackAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during configuration reload.");
        }
        finally
        {
            // Release the lock
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
    /// Creates a new dictionary and swaps the reference atomically.
    /// This ensures reader threads never see an empty or partially loaded cache.
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
