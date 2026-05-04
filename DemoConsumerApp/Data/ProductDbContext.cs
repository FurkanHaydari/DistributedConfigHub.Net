using DemoConsumerApp.Models;
using DistributedConfigHub.Client.Interfaces;
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
        // Do not intervene if options are already configured externally like DatabaseInitializer
        if (optionsBuilder.IsConfigured) return;
        
        // STEP 1: Try fetching from Dynamic Hub
        var connectionString = configSdk.GetString("MainDatabase");
        
        // STEP 2: If Hub is empty, use FallbackConnection from appsettings.json
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
