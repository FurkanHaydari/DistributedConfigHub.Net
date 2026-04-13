using DistributedConfigHub.Application.Interfaces;
using DistributedConfigHub.Domain.Entities;
using DistributedConfigHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DistributedConfigHub.Infrastructure.Repositories;

public class ConfigurationRepository(ConfigDbContext dbContext) : IConfigurationRepository
{
    public async Task<ConfigurationRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Configurations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ConfigurationRecord>> GetConfigurationsAsync(string applicationName, string? environment, CancellationToken cancellationToken = default)
    {
        return await dbContext.Configurations
            .AsNoTracking()
            .Where(c => c.ApplicationName == applicationName &&
                       (string.IsNullOrEmpty(environment) || c.Environment == environment) &&
                       c.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ConfigurationRecord>> GetDeletedConfigurationsAsync(string applicationName, string? environment, CancellationToken cancellationToken = default)
    {
        return await dbContext.Configurations
            .AsNoTracking()
            .Where(c => c.ApplicationName == applicationName &&
                       (string.IsNullOrEmpty(environment) || c.Environment == environment) &&
                       !c.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ConfigurationRecord record, CancellationToken cancellationToken = default)
    {
        await dbContext.Configurations.AddAsync(record, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ConfigurationRecord record, CancellationToken cancellationToken = default)
    {
        dbContext.Configurations.Update(record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ConfigurationRecord record, CancellationToken cancellationToken = default)
    {
        dbContext.Configurations.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
