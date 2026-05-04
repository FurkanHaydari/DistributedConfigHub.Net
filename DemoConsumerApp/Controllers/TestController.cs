using System.Diagnostics;
using DistributedConfigHub.Client.Interfaces;
using DistributedConfigHub.Client.Models;
using Microsoft.AspNetCore.Mvc;

namespace DemoConsumerApp.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController(
    IConfigSdkService configService, 
    DistributedConfigOptions configOptions,
    IWebHostEnvironment env,
    ILogger<TestController> logger) : ControllerBase
{
    private static readonly DateTime AppStartTime = Process.GetCurrentProcess().StartTime;

    [HttpGet]
    public IActionResult Get()
    {
        var paymentApiUrl = configService.GetString("ExternalPaymentApiUrl") ?? "https://pay.default-enterprise.com/api";
        var maxTransactions = configService.GetInt("MaxConcurrentTransactions", 100);
        var isMaintenance = configService.GetBoolean("IsMaintenanceModeEnabled", false);
        var dbConnection = configService.GetString("MainDatabase") ?? "unknown";
        var uptime = DateTime.Now - AppStartTime;

        var data = new
        {
            Message = "Demo Consumer App - Current Configuration Memory Status",
            Timestamp = DateTimeOffset.UtcNow,
            Uptime = uptime.ToString(@"hh\:mm\:ss"),
            StartTime = AppStartTime.ToString("yyyy-MM-dd HH:mm:ss"),
            Environment = new
            {
                AspNetCore = env.EnvironmentName,
                ConfigHub = configOptions.Environment
            },
            Configs = new
            {
                PaymentApiUrl = paymentApiUrl,
                MaxConcurrentTransactions = maxTransactions,
                IsMaintenanceModeEnabled = isMaintenance,
                Database = dbConnection
            }
        };

        logger.LogDebug("Current in-memory config state: {@Configs}", data.Configs);

        return Ok(data);
    }
}
