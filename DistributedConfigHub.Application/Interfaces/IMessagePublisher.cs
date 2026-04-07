namespace DistributedConfigHub.Application.Interfaces;

public interface IMessagePublisher
{
    Task PublishConfigurationUpdatedEventAsync(string applicationName, string environment, CancellationToken cancellationToken = default);
}
