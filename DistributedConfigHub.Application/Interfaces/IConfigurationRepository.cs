using DistributedConfigHub.Domain.Entities;

namespace DistributedConfigHub.Application.Interfaces;

public interface IConfigurationRepository
{
    Task<ConfigurationRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ConfigurationRecord>> GetConfigurationsAsync(string applicationName, string? environment, CancellationToken cancellationToken = default);
    Task<IEnumerable<ConfigurationRecord>> GetDeletedConfigurationsAsync(string applicationName, string? environment, CancellationToken cancellationToken = default);
    Task AddAsync(ConfigurationRecord record, CancellationToken cancellationToken = default);
    Task UpdateAsync(ConfigurationRecord record, CancellationToken cancellationToken = default);
    Task DeleteAsync(ConfigurationRecord record, CancellationToken cancellationToken = default);
    Task<ConfigurationRecord?> GetByNameAsync(string name, string applicationName, string environment, CancellationToken cancellationToken = default);
}
