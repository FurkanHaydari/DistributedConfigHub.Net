using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using DistributedConfigHub.Client.Interfaces;
using DistributedConfigHub.Client.Models;

namespace DistributedConfigHub.Client.Services;

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

            // 2. Success Case - Geçerli veri gelip gelmediği kontrolü
            if (response != null && response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var items = JsonSerializer.Deserialize<List<ConfigurationItem>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                // LKGC Guard: Boş veri gelirse ve elimizde zaten veri varsa, "Son Bilinen İyi Veriyi" (Last Known Good) koru
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
                    
                    // Fallback dosyasını sadece API'den başarılı ve dolu veri geldiğinde güncelle
                    await File.WriteAllTextAsync(options.FallbackFilePath, content, cancellationToken);
                    logger.LogInformation("Configurations successfully loaded from API and cached to {FallbackFile}.", options.FallbackFilePath);
                    return;
                }
            }
            
            // 3. Failure Case - API başarısız ise hafızadaki veriyi koru veya fallback'e bak
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
            // Kilidi serbest bırak
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
