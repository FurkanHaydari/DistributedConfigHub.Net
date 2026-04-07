namespace DistributedConfigHub.Client;

public record ConfigurationItem(
    string Name,
    string Type,
    string Value,
    string ApplicationName,
    string Environment,
    bool IsActive
);
