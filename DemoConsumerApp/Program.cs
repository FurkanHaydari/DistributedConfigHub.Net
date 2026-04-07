using DistributedConfigHub.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDistributedConfigHub(options =>
{
    builder.Configuration.GetSection("DistributedConfig").Bind(options);
});

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

app.Run();
