using DistributedConfigHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DistributedConfigHub.Infrastructure.Data;

public class ConfigDbContext(DbContextOptions<ConfigDbContext> options) : DbContext(options)
{
    public DbSet<ConfigurationRecord> Configurations { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConfigDbContext).Assembly);
    }
}
