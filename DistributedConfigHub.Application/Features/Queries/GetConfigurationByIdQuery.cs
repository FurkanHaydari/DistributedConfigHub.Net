using DistributedConfigHub.Application.DTOs;
using DistributedConfigHub.Application.Interfaces;
using MediatR;

namespace DistributedConfigHub.Application.Features.Queries;

public record GetConfigurationByIdQuery(Guid Id, string CallerApplicationName = "") : IRequest<ConfigurationDto?>;

public class GetConfigurationByIdQueryHandler(IConfigurationRepository repository) : IRequestHandler<GetConfigurationByIdQuery, ConfigurationDto?>
{
    public async Task<ConfigurationDto?> Handle(GetConfigurationByIdQuery request, CancellationToken cancellationToken)
    {
        var record = await repository.GetByIdAsync(request.Id, cancellationToken);
        
        if (record is null) return null;

        if (!string.Equals(record.ApplicationName, request.CallerApplicationName, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Security Violation: You cannot read configuration belonging to another service!");
        
        return new ConfigurationDto(record.Id, record.Name, record.Type, record.Value, record.ApplicationName, record.Environment, record.IsActive);
    }
}