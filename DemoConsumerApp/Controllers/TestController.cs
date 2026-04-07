using DistributedConfigHub.Client;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DemoConsumerApp.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController(IConfigSdkService configService) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        // Başlangıçta IConfigSdkService tarafından RabbitMQ Hosted Service'da yüklenmiş olan belleği okuyoruz.
        // Konfigürasyon yönetim paneli (API) üzerinden değer güncellendiğinde buradaki sonuçlar ANINDA değişecektir.
        var paymentGatewayUrl = configService.GetString("PaymentGatewayUrl") ?? "https://varsayilan-gateway.com/api";
        var maxTransactions = configService.GetInt("MaxIstanbulKartTransactionsPerMin", 100);
        var isMaintenance = configService.GetBoolean("IsMaintenanceModeEnabled", false);

        var data = new
        {
            Message = "Demo Consumer App - Güncel İBB Konfigürasyon Bellek Durumu",
            Timestamp = DateTimeOffset.UtcNow,
            Configs = new
            {
                PaymentGatewayUrl = paymentGatewayUrl,
                MaxIstanbulKartTransactionsPerMin = maxTransactions,
                IsMaintenanceModeEnabled = isMaintenance
            }
        };

        // Bu değerler ekrana (endpoint'e) ve ayrıca sunucu konsoluna JSON olarak yazdırılır.
        Console.WriteLine($"[TestController] Anlık bellek durumu okundu: {JsonSerializer.Serialize(data.Configs)}");

        return Ok(data);
    }
}
