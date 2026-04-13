using DistributedConfigHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistributedConfigHub.Infrastructure.Data.Configurations;

public class ConfigurationRecordConfiguration : IEntityTypeConfiguration<ConfigurationRecord>
{
    public void Configure(EntityTypeBuilder<ConfigurationRecord> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Type).IsRequired().HasConversion<string>();
        builder.Property(x => x.Value).IsRequired();
        builder.Property(x => x.ApplicationName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Environment).IsRequired().HasMaxLength(50);
        builder.Property(x => x.IsActive).IsRequired();
        
        builder.HasIndex(x => new { x.Name, x.ApplicationName, x.Environment }).IsUnique();
        
        // Seed Data
        var seedDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        builder.HasData(
            // SERVICE-A - DEV
            new { 
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), 
                Name = "ExternalPaymentApiUrl", 
                Type = DistributedConfigHub.Domain.Enums.ConfigurationType.String, 
                Value = "https://dev-pay.enterprise.com", 
                ApplicationName = "SERVICE-A", 
                Environment = "dev", 
                IsActive = true, 
                CreatedAt = seedDate,
                CreatedBy = "system"
            },
            new { 
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), 
                Name = "MaxConcurrentTransactions", 
                Type = DistributedConfigHub.Domain.Enums.ConfigurationType.Int, 
                Value = "100", 
                ApplicationName = "SERVICE-A", 
                Environment = "dev", 
                IsActive = true, 
                CreatedAt = seedDate,
                CreatedBy = "system"
            },
            new { 
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), 
                Name = "IsMaintenanceModeEnabled", 
                Type = DistributedConfigHub.Domain.Enums.ConfigurationType.Boolean, 
                Value = "true", 
                ApplicationName = "SERVICE-A", 
                Environment = "dev", 
                IsActive = true, 
                CreatedAt = seedDate,
                CreatedBy = "system"
            },
            new { 
                Id = Guid.Parse("00000000-0000-0000-0000-000000000101"), 
                Name = "MainDatabase", 
                Type = DistributedConfigHub.Domain.Enums.ConfigurationType.String, 
                Value = "Host=postgres;Database=db_alpha;Username=postgres;Password=postgres", 
                ApplicationName = "SERVICE-A", 
                Environment = "dev", 
                IsActive = true, 
                CreatedAt = seedDate,
                CreatedBy = "system"
            },

            // SERVICE-A - STAGING
            new { 
                Id = Guid.Parse("00000000-0000-0000-0000-000000000004"), 
                Name = "ExternalPaymentApiUrl", 
                Type = DistributedConfigHub.Domain.Enums.ConfigurationType.String, 
                Value = "https://test-pay.enterprise.com", 
                ApplicationName = "SERVICE-A", 
                Environment = "staging", 
                IsActive = true, 
                CreatedAt = seedDate,
                CreatedBy = "system"
            },
            new { 
                Id = Guid.Parse("00000000-0000-0000-0000-000000000005"), 
                Name = "MaxConcurrentTransactions", 
                Type = DistributedConfigHub.Domain.Enums.ConfigurationType.Int, 
                Value = "1000", 
                ApplicationName = "SERVICE-A", 
                Environment = "staging", 
                IsActive = true, 
                CreatedAt = seedDate,
                CreatedBy = "system"
            },
            new { 
                Id = Guid.Parse("00000000-0000-0000-0000-000000000006"), 
                Name = "IsMaintenanceModeEnabled", 
                Type = DistributedConfigHub.Domain.Enums.ConfigurationType.Boolean, 
                Value = "false", 
                ApplicationName = "SERVICE-A", 
                Environment = "staging", 
                IsActive = true, 
                CreatedAt = seedDate,
                CreatedBy = "system"
            },
            new { 
                Id = Guid.Parse("00000000-0000-0000-0000-000000000102"), 
                Name = "MainDatabase", 
                Type = DistributedConfigHub.Domain.Enums.ConfigurationType.String, 
                Value = "Host=postgres;Database=db_alpha;Username=postgres;Password=postgres", 
                ApplicationName = "SERVICE-A", 
                Environment = "staging", 
                IsActive = true, 
                CreatedAt = seedDate,
                CreatedBy = "system"
            },

            // SERVICE-A - PROD
            new { 
                Id = Guid.Parse("00000000-0000-0000-0000-000000000007"), 
                Name = "ExternalPaymentApiUrl", 
                Type = DistributedConfigHub.Domain.Enums.ConfigurationType.String, 
                Value = "https://pay.enterprise.com", 
                ApplicationName = "SERVICE-A", 
                Environment = "prod", 
                IsActive = true, 
                CreatedAt = seedDate,
                CreatedBy = "system"
            },
            new { 
                Id = Guid.Parse("00000000-0000-0000-0000-000000000008"), 
                Name = "MaxConcurrentTransactions", 
                Type = DistributedConfigHub.Domain.Enums.ConfigurationType.Int, 
                Value = "50000", 
                ApplicationName = "SERVICE-A", 
                Environment = "prod", 
                IsActive = true, 
                CreatedAt = seedDate,
                CreatedBy = "system"
            },
            new { 
                Id = Guid.Parse("00000000-0000-0000-0000-000000000009"), 
                Name = "IsMaintenanceModeEnabled", 
                Type = DistributedConfigHub.Domain.Enums.ConfigurationType.Boolean, 
                Value = "false", 
                ApplicationName = "SERVICE-A", 
                Environment = "prod", 
                IsActive = true, 
                CreatedAt = seedDate,
                CreatedBy = "system"
            },
            new { 
                Id = Guid.Parse("00000000-0000-0000-0000-000000000103"), 
                Name = "MainDatabase", 
                Type = DistributedConfigHub.Domain.Enums.ConfigurationType.String, 
                Value = "Host=postgres;Database=db_alpha;Username=postgres;Password=postgres", 
                ApplicationName = "SERVICE-A", 
                Environment = "prod", 
                IsActive = true, 
                CreatedAt = seedDate,
                CreatedBy = "system"
            }
        );
    }
}