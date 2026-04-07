using DistributedConfigHub.Domain.Enums;

namespace DistributedConfigHub.Application.DTOs;

public record ConfigurationDto(
    Guid Id,
    string Name,
    ConfigurationType Type,
    string Value,
    string ApplicationName,
    string Environment,
    bool IsActive
);
