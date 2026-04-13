using DemoConsumerApp.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DemoConsumerApp.Data;

public interface IDatabaseInitializer
{
    Task InitializeAsync();
}

public class DatabaseInitializer(IConfiguration configuration, ILogger<DatabaseInitializer> logger) : IDatabaseInitializer
{
    // Demo senaryosu için iki veritabanı: Hot-swap ile geçiş yapılacak
    private static readonly string[] TargetDatabases = ["db_alpha", "db_beta"];

    public async Task InitializeAsync()
    {
        var baseConnectionString = configuration.GetConnectionString("FallbackConnection");
        if (string.IsNullOrEmpty(baseConnectionString))
        {
            logger.LogWarning("FallbackConnection not found in configuration. Skipping database initialization.");
            return;
        }

        foreach (var dbName in TargetDatabases)
        {
            try
            {
                var builder = new NpgsqlConnectionStringBuilder(baseConnectionString) { Database = dbName };
                var connString = builder.ToString();

                // 1. ADIM: Veritabanı yoksa PostgreSQL'de yarat
                await EnsureDatabaseExistsAsync(baseConnectionString, dbName);

                // 2. ADIM: Tablolar + Seed data
                var options = new DbContextOptionsBuilder<SeedDbContext>()
                    .UseNpgsql(connString)
                    .Options;

                await using var context = new SeedDbContext(options);
                await context.Database.EnsureCreatedAsync();

                if (!await context.Products.AnyAsync())
                {
                    SeedProducts(context, dbName);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Database '{DatabaseName}' initialized with seed data.", dbName);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize database '{DatabaseName}'.", dbName);
            }
        }
    }

    private static void SeedProducts(SeedDbContext context, string dbName)
    {
        var products = dbName switch
        {
            "db_alpha" =>
            [
                new Product { Name = "Alpha Laptop Pro", Description = "16\" M3 Ultra, 64GB RAM, 2TB SSD", Price = 89999 },
                new Product { Name = "Alpha Phone X", Description = "6.7\" OLED, 256GB, 5G destekli amiral gemisi", Price = 54999 },
                new Product { Name = "Alpha Wireless Earbuds", Description = "ANC, 30 saat pil, IPX5 su geçirmez", Price = 4999 },
                new Product { Name = "Alpha Monitor 4K", Description = "32\" IPS, HDR600, USB-C hub, 120Hz", Price = 24999 },
                new Product { Name = "Alpha Keyboard MX", Description = "Mekanik, hot-swap switch, RGB backlight", Price = 3499 }
            ],
            "db_beta" =>
            [
                new Product { Name = "Beta Smartwatch Ultra", Description = "GPS, kalp ritmi, SpO2, 14 gün pil ömrü", Price = 12999 },
                new Product { Name = "Beta Tablet Air", Description = "11\" Liquid Retina, M2 çip, Apple Pencil uyumlu", Price = 32999 },
                new Product { Name = "Beta Fitness Band", Description = "AMOLED ekran, uyku takibi, su geçirmez", Price = 2499 },
                new Product { Name = "Beta Power Bank", Description = "20000mAh, 65W hızlı şarj, USB-C PD", Price = 1999 }
            ],
            _ => Array.Empty<Product>()
        };

        context.Products.AddRange(products);
    }

    private async Task EnsureDatabaseExistsAsync(string baseConnectionString, string targetDbName)
    {
        var builder = new NpgsqlConnectionStringBuilder(baseConnectionString) { Database = "postgres" };

        await using var conn = new NpgsqlConnection(builder.ToString());
        await conn.OpenAsync();

        await using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = @dbName";
        checkCmd.Parameters.AddWithValue("dbName", targetDbName);
        var exists = await checkCmd.ExecuteScalarAsync();

        if (exists is null)
        {
            await using var createCmd = conn.CreateCommand();
            createCmd.CommandText = $"CREATE DATABASE \"{targetDbName}\"";
            await createCmd.ExecuteNonQueryAsync();
            logger.LogInformation("Database '{DatabaseName}' created on PostgreSQL server.", targetDbName);
        }
    }

    private class SeedDbContext(DbContextOptions<SeedDbContext> options) : DbContext(options)
    {
        public DbSet<Product> Products => Set<Product>();
    }
}
