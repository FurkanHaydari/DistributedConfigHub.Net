using DistributedConfigHub.Application.Interfaces;
using MediatR;

namespace DistributedConfigHub.Application.Features.Commands;

public record DeleteConfigurationCommand(Guid Id, string CallerApplicationName = "") : IRequest<bool>;

public class DeleteConfigurationCommandHandler(
    IConfigurationRepository repository, 
    IMessagePublisher messagePublisher) 
    : IRequestHandler<DeleteConfigurationCommand, bool>
{
    public async Task<bool> Handle(DeleteConfigurationCommand request, CancellationToken cancellationToken)
    {
        var record = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (record is null) 
            throw new KeyNotFoundException($"Configuration with Id {request.Id} not found.");

        if (!string.Equals(record.ApplicationName, request.CallerApplicationName, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Security Violation: Service {request.CallerApplicationName} cannot delete a record belonging to service {record.ApplicationName}!");
    
        record.Deactivate("admin");
        await repository.UpdateAsync(record, cancellationToken);

        await messagePublisher.PublishConfigurationUpdatedEventAsync(record.ApplicationName, record.Environment, cancellationToken);

        return true;
    }
}