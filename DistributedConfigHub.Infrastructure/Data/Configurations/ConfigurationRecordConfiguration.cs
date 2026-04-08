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
        
        // IBB Seed Data
        var seedDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        builder.HasData(
            new ConfigurationRecord(Guid.Parse("00000000-0000-0000-0000-000000000001"), "PaymentGatewayUrl", DistributedConfigHub.Domain.Enums.ConfigurationType.String, "https://dev-odeme.ibb.istanbul", "SERVICE-A", "dev", true) { CreatedAt = seedDate },
            new ConfigurationRecord(Guid.Parse("00000000-0000-0000-0000-000000000002"), "MaxIstanbulKartTransactionsPerMin", DistributedConfigHub.Domain.Enums.ConfigurationType.Int, "100", "SERVICE-A", "dev", true) { CreatedAt = seedDate },
            new ConfigurationRecord(Guid.Parse("00000000-0000-0000-0000-000000000003"), "IsMaintenanceModeEnabled", DistributedConfigHub.Domain.Enums.ConfigurationType.Boolean, "true", "SERVICE-A", "dev", true) { CreatedAt = seedDate },

            new ConfigurationRecord(Guid.Parse("00000000-0000-0000-0000-000000000004"), "PaymentGatewayUrl", DistributedConfigHub.Domain.Enums.ConfigurationType.String, "https://test-odeme.ibb.istanbul", "SERVICE-A", "staging", true) { CreatedAt = seedDate },
            new ConfigurationRecord(Guid.Parse("00000000-0000-0000-0000-000000000005"), "MaxIstanbulKartTransactionsPerMin", DistributedConfigHub.Domain.Enums.ConfigurationType.Int, "1000", "SERVICE-A", "staging", true) { CreatedAt = seedDate },
            new ConfigurationRecord(Guid.Parse("00000000-0000-0000-0000-000000000006"), "IsMaintenanceModeEnabled", DistributedConfigHub.Domain.Enums.ConfigurationType.Boolean, "false", "SERVICE-A", "staging", true) { CreatedAt = seedDate },

            new ConfigurationRecord(Guid.Parse("00000000-0000-0000-0000-000000000007"), "PaymentGatewayUrl", DistributedConfigHub.Domain.Enums.ConfigurationType.String, "https://odeme.ibb.istanbul", "SERVICE-A", "prod", true) { CreatedAt = seedDate },
            new ConfigurationRecord(Guid.Parse("00000000-0000-0000-0000-000000000008"), "MaxIstanbulKartTransactionsPerMin", DistributedConfigHub.Domain.Enums.ConfigurationType.Int, "50000", "SERVICE-A", "prod", true) { CreatedAt = seedDate },
            new ConfigurationRecord(Guid.Parse("00000000-0000-0000-0000-000000000009"), "IsMaintenanceModeEnabled", DistributedConfigHub.Domain.Enums.ConfigurationType.Boolean, "false", "SERVICE-A", "prod", true) { CreatedAt = seedDate }
        );
    }
}
