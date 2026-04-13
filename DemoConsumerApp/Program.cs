using DistributedConfigHub.Client;
using DemoConsumerApp.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Dinamik DbContext ve Initializer kaydı
builder.Services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();
builder.Services.AddDbContext<ProductDbContext>();

builder.Services.AddDistributedConfigHub(options =>
{
    builder.Configuration.GetSection("DistributedConfig").Bind(options);
    
    // ASPNETCORE_ENVIRONMENT'tan beslen ve .NET standart env keywordlerini
    // Config Hub'daki temiz ve framework-agnostic keywordlere dönüştür
    options.Environment = builder.Environment.EnvironmentName switch
    {
        "Development" => "dev",
        "Staging"     => "staging",
        "Production"  => "prod",
        var custom     => custom.ToLowerInvariant()
    };
});

var app = builder.Build();

// SDK konfigürasyon her güncellendiğinde tüm snapshot'ı logla
var configOptions = app.Services.GetRequiredService<DistributedConfigOptions>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
configOptions.OnConfigurationUpdated = sdk =>
{
    foreach (var (key, value) in sdk.GetAll())
    {
        logger.LogInformation("  ↳ {Key} = {Value}", key, value);
    }
    return Task.CompletedTask;
};

// Sunum için uygulama başlarken her iki veritabanını da hazırla (yarat + seed)
var initializer = app.Services.GetRequiredService<IDatabaseInitializer>();
await initializer.InitializeAsync();

app.UseSwagger();
app.UseSwaggerUI();


app.UseAuthorization();
app.MapControllers();

app.Run();
