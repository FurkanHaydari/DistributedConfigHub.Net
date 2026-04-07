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
        
        // Seed Test Data
        builder.HasData(
            new ConfigurationRecord(Guid.Parse("11111111-1111-1111-1111-111111111111"), "SiteName", DistributedConfigHub.Domain.Enums.ConfigurationType.String, "Kadikoy Belediyesi Tech Ekibi", "SERVICE-A", "prod", true),
            new ConfigurationRecord(Guid.Parse("22222222-2222-2222-2222-222222222222"), "MaxUsers", DistributedConfigHub.Domain.Enums.ConfigurationType.Int, "15000", "SERVICE-A", "prod", true),
            new ConfigurationRecord(Guid.Parse("33333333-3333-3333-3333-333333333333"), "FeatureX_Enabled", DistributedConfigHub.Domain.Enums.ConfigurationType.Boolean, "true", "SERVICE-A", "prod", true)
        );
    }
}
