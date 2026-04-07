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

var builder = WebApplication.CreateBuilder(args);

// Add Exception Handlers (Modern approach replacing use-exception-handler middlewares)
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure FluentValidation from Assembly
builder.Services.AddValidatorsFromAssemblyContaining<CreateConfigurationCommandValidator>();

// Configure MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(IMessagePublisher).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

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

// Activate modern Exception Handlers
app.UseExceptionHandler();

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

public partial class Program { }

