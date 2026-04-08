using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DistributedConfigHub.Client;

public class ConfigSdkService : IConfigSdkService
{
    // volatile: Referans değişimi anında tüm thread'ler tarafından görülür (atomic swap)
    private volatile ConcurrentDictionary<string, ConfigurationItem> _cache = new();
    private readonly HttpClient _httpClient;
    private readonly DistributedConfigOptions _options;
    private readonly ILogger<ConfigSdkService> _logger;

    public ConfigSdkService(HttpClient httpClient, DistributedConfigOptions options, ILogger<ConfigSdkService> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
        
        // Sunucuya atılacak tüm isteklerde Kimlik Anahtarını HTTP Header olarak gönder!
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _options.ApiKey);
    }

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
        if (_cache.TryGetValue(key, out var item) && bool.TryParse(item.Value, out var parsed))
            return parsed;
        return defaultValue;
    }

    public async Task ReloadConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_options.ApiBaseUrl.TrimEnd('/')}/Configurations?applicationName={_options.ApplicationName}&environment={_options.Environment}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var items = JsonSerializer.Deserialize<List<ConfigurationItem>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (items is not null)
                {
                    SwapCache(items);
                    
                    await File.WriteAllTextAsync(_options.FallbackFilePath, content, cancellationToken);
                    _logger.LogInformation("Configurations successfully loaded from API and cached to {FallbackFile}.", _options.FallbackFilePath);
                    return;
                }
            }
            
            _logger.LogWarning("API responded with {StatusCode}. Attempting to read from local fallback.", response.StatusCode);
            await ReadFromFallbackAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch configurations from API. Attempting to use local fallback.");
            await ReadFromFallbackAsync(cancellationToken);
        }
    }

    private async Task ReadFromFallbackAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(_options.FallbackFilePath))
        {
            var content = await File.ReadAllTextAsync(_options.FallbackFilePath, cancellationToken);
            var items = JsonSerializer.Deserialize<List<ConfigurationItem>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (items is not null)
            {
                SwapCache(items);
                _logger.LogInformation("Configurations loaded from local fallback file.");
            }
        }
        else
        {
            _logger.LogWarning("Local fallback file not found at {FallbackFile}. The configuration cache remains empty.", _options.FallbackFilePath);
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
