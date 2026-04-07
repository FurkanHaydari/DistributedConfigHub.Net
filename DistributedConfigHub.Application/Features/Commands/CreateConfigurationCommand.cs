using DistributedConfigHub.Application.Interfaces;
using DistributedConfigHub.Domain.Entities;
using DistributedConfigHub.Domain.Enums;
using MediatR;

namespace DistributedConfigHub.Application.Features.Commands;

public record CreateConfigurationCommand(string Name, ConfigurationType Type, string Value, string ApplicationName, string Environment) : IRequest<Guid>;

public class CreateConfigurationCommandHandler(IConfigurationRepository repository) : IRequestHandler<CreateConfigurationCommand, Guid>
{
    public async Task<Guid> Handle(CreateConfigurationCommand request, CancellationToken cancellationToken)
    {
        var newRecord = new ConfigurationRecord(
            Guid.NewGuid(), 
            request.Name, 
            request.Type, 
            request.Value, 
            request.ApplicationName, 
            request.Environment, 
            true);

        await repository.AddAsync(newRecord, cancellationToken);
        return newRecord.Id;
    }
}
