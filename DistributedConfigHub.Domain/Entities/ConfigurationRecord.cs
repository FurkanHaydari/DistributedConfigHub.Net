using DistributedConfigHub.Domain.Enums;
using System.Globalization;

namespace DistributedConfigHub.Domain.Entities;

public class ConfigurationRecord : BaseAuditableEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public ConfigurationType Type { get; private set; }
    public string Value { get; private set; } = default!;
    public string ApplicationName { get; private set; } = default!;
    public string Environment { get; private set; } = default!;
    public bool IsActive { get; private set; }

    protected ConfigurationRecord() { }

    public ConfigurationRecord(string name, ConfigurationType type, string value, string applicationName, string environment, string createdBy = "system")
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be empty.");

        // Nesne yaratılırken kuralı işlet
        if (!IsValidValueForType(type, value))
            throw new InvalidOperationException($"The provided value '{value}' is not a valid format for type '{type}'.");

        Id = Guid.NewGuid();
        Name = name;
        Type = type;
        Value = value;
        ApplicationName = applicationName;
        Environment = environment;
        IsActive = true; 
        
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateValue(string newValue, string updatedBy = "system")
    {
        if (string.IsNullOrWhiteSpace(newValue))
            throw new ArgumentException("Value cannot be empty.");

        if (!IsValidValueForType(this.Type, newValue))
            throw new InvalidOperationException($"The provided value '{newValue}' is not a valid format for type '{this.Type}'.");
        
        Value = newValue;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate(string updatedBy = "system")
    {
        IsActive = false;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate(string updatedBy = "system")
    {
        IsActive = true;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static bool IsValidValueForType(ConfigurationType type, string value)
    {
        return type switch
        {
            ConfigurationType.String => true,
            ConfigurationType.Int => int.TryParse(value, out _),
            ConfigurationType.Double => double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _),
            ConfigurationType.Boolean => bool.TryParse(value, out _) || value == "1" || value == "0",
            _ => false
        };
    }
}