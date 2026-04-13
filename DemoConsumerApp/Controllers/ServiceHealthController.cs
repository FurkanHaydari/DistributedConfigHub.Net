using System.Diagnostics;
using DemoConsumerApp.Data;
using DistributedConfigHub.Client;
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
        string dbName = "Unknown";
        string dbConnectStatus = "Unknown";

        try
        {
            // Veritabanına gerçekten bağlanabiliyor muyuz?
            var conn = context.Database.GetDbConnection();
            dbName = conn.Database;
            await context.Database.OpenConnectionAsync();
            dbConnectStatus = "Connected ✅";
            await context.Database.CloseConnectionAsync();
        }
        catch (Exception ex)
        {
            dbConnectStatus = $"Error ❌: {ex.Message}";
        }

        var isHealthy = dbConnectStatus.Contains("✅");

        var healthResponse = new
        {
            Service = "Consumer Demo App",
            Status = isHealthy ? "Healthy ✅" : "Degraded ⚠️",
            Uptime = (DateTime.Now - _startTime).ToString(@"hh\:mm\:ss"),
            StartTime = _startTime.ToString("HH:mm:ss"),
            RuntimeConfiguration = new
            {
                ResolvedDatabaseName = dbName,
                DatabaseStatus = dbConnectStatus
            },
            Meta = new
            {
                Maintainer = "Enterprise Dist. Systems",
                System = "DistributedConfigHub"
            }
        };

        return isHealthy ? Ok(healthResponse) : StatusCode(503, healthResponse);
    }
}
