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

using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Exception Handlers
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(opts => 
    {
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new OpenApiInfo { Title = "Distributed Config Hub API", Version = "v1" };
        
        var apiKeyScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "Service-Key",
            Description = "Service Key authentication is required."
        };

        document.Components ??= new OpenApiComponents();
        document.AddComponent("Service-Key", apiKeyScheme);

        var securityRequirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Service-Key", document)] = new List<string>()
        };

        if (document.Paths != null)
        {
            foreach (var path in document.Paths.Values)
            {
                if (path.Operations != null)
                {
                    foreach (var operation in path.Operations.Values)
                    {
                        operation.Security ??= new List<OpenApiSecurityRequirement>();
                        operation.Security.Add(securityRequirement);
                    }
                }
            }
        }

        return Task.CompletedTask;
    });
});

// Configure FluentValidation from Assembly
builder.Services.AddValidatorsFromAssemblyContaining<CreateConfigurationCommandValidator>();

// Register Custom Action Filters
builder.Services.AddScoped<ApiKeyAuthorizeAttribute>();

// Configure MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(IMessagePublisher).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TenantAuthorizationBehavior<,>));
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
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

var app = builder.Build();

// Veritabanı tablolarını ve seed data'yı oluştur 
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ConfigDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseExceptionHandler();

app.MapOpenApi(); 
app.MapScalarApiReference(); 

app.UseHttpsRedirection();

// Admin Panel için statik dosya sunumu (wwwroot/index.html)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }