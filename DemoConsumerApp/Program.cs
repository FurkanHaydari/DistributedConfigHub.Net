using DistributedConfigHub.Client;
using DemoConsumerApp.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Dinamik DbContext ve Initializer kaydı
builder.Services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();
builder.Services.AddDbContext<ProductDbContext>();

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

// Sunum için uygulama başlarken her iki veritabanını da hazırla (yarat + seed)
var initializer = app.Services.GetRequiredService<IDatabaseInitializer>();
await initializer.InitializeAsync();


app.UseAuthorization();
app.MapControllers();

app.Run();
