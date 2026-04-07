using DistributedConfigHub.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDistributedConfigHub(options =>
{
    options.ApplicationName = "SERVICE-A";
    options.Environment = "prod";
    options.ApiBaseUrl = "http://localhost:5173"; // Address of our ConfigHub Api
    options.FallbackFilePath = "local-fallback-config.json";
    
    // RabbitMQ Credentials
    options.RabbitMqHostName = "localhost";
});

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

app.Run();
