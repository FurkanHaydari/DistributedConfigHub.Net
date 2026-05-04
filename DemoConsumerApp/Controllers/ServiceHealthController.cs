using System.Diagnostics;
using DemoConsumerApp.Data;
using DistributedConfigHub.Client.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoConsumerApp.Controllers; 

[ApiController]
[Route("[controller]")]
public class ServiceHealthController(IConfigSdkService configSdk, ProductDbContext context) : ControllerBase
{
    private static readonly DateTime _startTime = Process.GetCurrentProcess().StartTime;

    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        // 1. Database Check
        string dbName = "Unknown";
        string dbConnectStatus = "Unknown";
        bool isDbHealthy = false;

        try
        {
            var conn = context.Database.GetDbConnection();
            dbName = conn.Database;
            await context.Database.OpenConnectionAsync();
            dbConnectStatus = "Connected ✅";
            isDbHealthy = true;
            await context.Database.CloseConnectionAsync();
        }
        catch (Exception ex)
        {
            dbConnectStatus = $"Error ❌: {ex.Message}";
        }

        // 2. SDK (Configuration) Check
        var configs = configSdk.GetAll();
        bool isSdkHealthy = configs.Any(); // If there are no records in the dictionary, SDK failed to fetch settings.
        string sdkStatus = isSdkHealthy 
            ? $"Healthy ✅ ({configs.Count} items loaded)" 
            : "Degraded ❌ (No configurations found in cache)";

        // 3. Overall Health Status (If both are healthy, system is up)
        var isSystemHealthy = isDbHealthy && isSdkHealthy;

        var healthResponse = new
        {
            Service = "Consumer Demo App",
            Status = isSystemHealthy ? "Healthy ✅" : "Degraded ⚠️",
            Uptime = (DateTime.Now - _startTime).ToString(@"hh\:mm\:ss"),
            StartTime = _startTime.ToString("HH:mm:ss"),
            Dependencies = new
            {
                Database = new
                {
                    ResolvedName = dbName,
                    Status = dbConnectStatus
                },
                ConfigurationHubSdk = new
                {
                    Status = sdkStatus
                }
            },
            Meta = new
            {
                Maintainer = "Enterprise Dist. Systems",
                System = "DistributedConfigHub Consumer"
            }
        };

        // If system is not fully healthy, return 503 Service Unavailable (Crucial for systems like Kubernetes)
        return isSystemHealthy ? Ok(healthResponse) : StatusCode(503, healthResponse);
    }
}