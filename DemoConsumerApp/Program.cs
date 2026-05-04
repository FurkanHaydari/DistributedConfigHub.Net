using DistributedConfigHub.Client;
using DistributedConfigHub.Client.Interfaces;
using DistributedConfigHub.Client.Models;
using DemoConsumerApp.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Dynamic DbContext and Initializer registration
builder.Services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();
builder.Services.AddDbContext<ProductDbContext>();

builder.Services.AddDistributedConfigHub(options =>
{
    builder.Configuration.GetSection("DistributedConfig").Bind(options);
    
    // ASPNETCORE_ENVIRONMENT'tan beslen ve .NET standart env keywordlerini
    // Convert to clean and framework-agnostic keywords in Config Hub
    options.Environment = builder.Environment.EnvironmentName switch
    {
        "Development" => "dev",
        "Staging"     => "staging",
        "Production"  => "prod",
        var custom     => custom.ToLowerInvariant()
    };
});

var app = builder.Build();

// Log entire snapshot every time SDK configuration is updated
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

// For demo purposes, prepare both databases when app starts (create + seed)
var initializer = app.Services.GetRequiredService<IDatabaseInitializer>();
await initializer.InitializeAsync();

app.UseSwagger();
app.UseSwaggerUI();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();

app.Run();
