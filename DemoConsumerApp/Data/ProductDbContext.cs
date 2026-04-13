using DemoConsumerApp.Models;
using DistributedConfigHub.Client;
using Microsoft.EntityFrameworkCore;

namespace DemoConsumerApp.Data;

public class ProductDbContext(
    DbContextOptions<ProductDbContext> options, 
    IConfigSdkService configSdk, 
    IConfiguration configuration,
    ILogger<ProductDbContext> logger) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // DatabaseInitializer gibi dışarıdan options zaten yapılandırılmışsa tekrar müdahale etme
        if (optionsBuilder.IsConfigured) return;
        
        // 1. ADIM: Dinamik Hub'dan çekmeyi dene
        var connectionString = configSdk.GetString("MainDatabase");
        
        // 2. ADIM: Eğer Hub boşsa appsettings.json'daki FallbackConnection'ı kullan
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = configuration.GetConnectionString("FallbackConnection");
            logger.LogWarning("Dynamic configuration not found! Using fallback from appsettings.json.");
        }
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            optionsBuilder.UseNpgsql(connectionString);
        }
    }
}
