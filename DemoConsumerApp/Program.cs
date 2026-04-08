using DistributedConfigHub.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDistributedConfigHub(options =>
{
    builder.Configuration.GetSection("DistributedConfig").Bind(options);
    
    // Konfigürasyon güncellendiğinde çağrılacak geri bildirim fonksiyonu
    options.OnConfigurationUpdated = configService =>
    {
        var paymentGw = configService.GetString("PaymentGatewayUrl");
        var limit = configService.GetInt("MaxIstanbulKartTransactionsPerMin");
        var maintenance = configService.GetBoolean("IsMaintenanceModeEnabled");
        Console.WriteLine($"[EVENT BAŞARILI] Bellek güncellendi → Gateway: {paymentGw}, Limit: {limit}, Bakım: {maintenance}");
    };
});

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

app.Run();
