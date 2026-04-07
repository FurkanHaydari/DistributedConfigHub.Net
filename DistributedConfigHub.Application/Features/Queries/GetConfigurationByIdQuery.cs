using DistributedConfigHub.Application.DTOs;
using DistributedConfigHub.Application.Interfaces;
using MediatR;

namespace DistributedConfigHub.Application.Features.Queries;

public record GetConfigurationByIdQuery(Guid Id) : IRequest<ConfigurationDto?>;

public class GetConfigurationByIdQueryHandler(IConfigurationRepository repository) : IRequestHandler<GetConfigurationByIdQuery, ConfigurationDto?>
{
    public async Task<ConfigurationDto?> Handle(GetConfigurationByIdQuery request, CancellationToken cancellationToken)
    {
        var record = await repository.GetByIdAsync(request.Id, cancellationToken);
        
        if (record is null) return null;
        
        return new ConfigurationDto(record.Id, record.Name, record.Type, record.Value, record.ApplicationName, record.Environment, record.IsActive);
    }
}
