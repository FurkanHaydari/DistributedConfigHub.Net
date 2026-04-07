using DistributedConfigHub.Application.Interfaces;
using DistributedConfigHub.Infrastructure.Data;
using DistributedConfigHub.Infrastructure.Messaging;
using DistributedConfigHub.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure MediatR (This will scan the Application assembly where handlers exist)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(IMessagePublisher).Assembly));

// Configure Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? "Host=localhost;Database=ConfigHubDb;Username=postgres;Password=postgres";
                       
builder.Services.AddDbContext<ConfigDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure Infrastructure Dependencies
builder.Services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
// RabbitMqPublisher is stateless, Singleton is perfectly fine here
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
