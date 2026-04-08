using DistributedConfigHub.Application.Interfaces;
using DistributedConfigHub.Infrastructure.Data;
using DistributedConfigHub.Infrastructure.Messaging;
using DistributedConfigHub.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using DistributedConfigHub.Application.Behaviors;
using DistributedConfigHub.Application.Features.Commands;
using DistributedConfigHub.Api.Infrastructure.ExceptionHandling;
using MediatR;
using DistributedConfigHub.Api.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add Exception Handlers (Modern approach replacing use-exception-handler middlewares)
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(opts => 
    {
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure FluentValidation from Assembly
builder.Services.AddValidatorsFromAssemblyContaining<CreateConfigurationCommandValidator>();

// Register Custom Action Filters
builder.Services.AddScoped<ApiKeyAuthorizeAttribute>();

// Configure MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(IMessagePublisher).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// Configure Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? "Host=localhost;Database=ConfigHubDb;Username=postgres;Password=postgres";

builder.Services.AddSingleton<IAuditContextAccessor, DistributedConfigHub.Infrastructure.Data.Interceptors.AuditContextAccessor>();                       
builder.Services.AddSingleton<DistributedConfigHub.Infrastructure.Data.Interceptors.AuditInterceptor>();

builder.Services.AddDbContext<ConfigDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString);
    options.AddInterceptors(sp.GetRequiredService<DistributedConfigHub.Infrastructure.Data.Interceptors.AuditInterceptor>());
});

// Configure Infrastructure Dependencies
builder.Services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
builder.Services.AddScoped<IAuditLogRepository, DistributedConfigHub.Infrastructure.Data.Repositories.AuditLogRepository>();
// RabbitMqPublisher is stateless, Singleton is perfectly fine here
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

var app = builder.Build();

// Veritabanı tablolarını ve seed data'yı oluştur (ilk çalıştırmada)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ConfigDbContext>();
    dbContext.Database.EnsureCreated();
}

// Activate modern Exception Handlers
app.UseExceptionHandler();

// Swagger her ortamda açık (demo ve sunum kolaylığı için)
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Admin Panel için statik dosya sunumu (wwwroot/index.html)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }

