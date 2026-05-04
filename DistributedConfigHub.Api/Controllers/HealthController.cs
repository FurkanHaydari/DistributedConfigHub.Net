using DistributedConfigHub.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

namespace DistributedConfigHub.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController(ConfigDbContext dbContext, IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var result = new HealthReport
        {
            Timestamp = DateTimeOffset.UtcNow
        };

        // 1. PostgreSQL Connection Check
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            result.PostgreSql = new ServiceStatus { IsHealthy = true, Message = "Connected" };
        }
        catch (Exception ex)
        {
            result.PostgreSql = new ServiceStatus { IsHealthy = false, Message = ex.Message };
        }

        // 2. RabbitMQ Connection Check
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
                Port = int.TryParse(configuration["RabbitMQ:Port"], out var port) ? port : 5672,
                UserName = configuration["RabbitMQ:UserName"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest"
            };

            using var connection = await factory.CreateConnectionAsync(cancellationToken);
            result.RabbitMq = new ServiceStatus { IsHealthy = connection.IsOpen, Message = connection.IsOpen ? "Connected" : "Connection closed" };
        }
        catch (Exception ex)
        {
            result.RabbitMq = new ServiceStatus { IsHealthy = false, Message = ex.Message };
        }

        // 3. Registered Service(Application) List
        var allConfigs = await dbContext.Configurations
            .AsNoTracking()
            .Select(c => new { c.ApplicationName, c.Environment, c.IsActive })
            .ToListAsync(cancellationToken);

        result.RegisteredApplications = allConfigs
            .GroupBy(c => c.ApplicationName)
            .Select(g => new RegisteredApplication
            {
                Name = g.Key,
                Environments = g.Select(x => x.Environment).Distinct().Order().ToList(),
                ConfigCount = g.Count(x => x.IsActive)
            })
            .ToList();

        var isHealthy = result.PostgreSql.IsHealthy && result.RabbitMq.IsHealthy;
        return isHealthy ? Ok(result) : StatusCode(503, result);
    }
}

public class HealthReport
{
    public DateTimeOffset Timestamp { get; set; }
    public ServiceStatus PostgreSql { get; set; } = new();
    public ServiceStatus RabbitMq { get; set; } = new();
    public List<RegisteredApplication> RegisteredApplications { get; set; } = [];
}

public class ServiceStatus
{
    public bool IsHealthy { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class RegisteredApplication
{
    public string Name { get; set; } = string.Empty;
    public List<string> Environments { get; set; } = [];
    public int ConfigCount { get; set; }
}
