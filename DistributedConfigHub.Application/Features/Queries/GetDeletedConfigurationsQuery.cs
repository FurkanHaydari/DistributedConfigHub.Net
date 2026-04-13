using DistributedConfigHub.Application.DTOs;
using DistributedConfigHub.Application.Interfaces;
using MediatR;

namespace DistributedConfigHub.Application.Features.Queries;

public record GetDeletedConfigurationsQuery(string ApplicationName, string? Environment) : IRequest<IEnumerable<ConfigurationDto>>;

public class GetDeletedConfigurationsQueryHandler(IConfigurationRepository repository) : IRequestHandler<GetDeletedConfigurationsQuery, IEnumerable<ConfigurationDto>>
{
    public async Task<IEnumerable<ConfigurationDto>> Handle(GetDeletedConfigurationsQuery request, CancellationToken cancellationToken)
    {
        var records = await repository.GetDeletedConfigurationsAsync(request.ApplicationName, request.Environment, cancellationToken);
        
        return records.Select(r => new ConfigurationDto(r.Id, r.Name, r.Type, r.Value, r.ApplicationName, r.Environment, r.IsActive));
    }
}
