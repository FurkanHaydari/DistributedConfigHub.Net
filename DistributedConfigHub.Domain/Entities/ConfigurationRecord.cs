using DistributedConfigHub.Domain.Enums;

namespace DistributedConfigHub.Domain.Entities;

public class ConfigurationRecord(Guid id, string name, ConfigurationType type, string value, string applicationName, string environment, bool isActive) : BaseAuditableEntity
{
    public Guid Id { get; private set; } = id;
    public string Name { get; private set; } = name;
    public ConfigurationType Type { get; private set; } = type;
    public string Value { get; private set; } = value;
    public string ApplicationName { get; private set; } = applicationName;
    public string Environment { get; private set; } = environment;
    public bool IsActive { get; private set; } = isActive;

    public void UpdateValue(string newValue)
    {
        Value = newValue;
        UpdatedBy = "admin";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedBy = "admin";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedBy = "admin";
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
