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
    }
}
