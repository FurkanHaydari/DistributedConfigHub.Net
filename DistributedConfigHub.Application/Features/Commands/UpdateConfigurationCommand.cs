using DistributedConfigHub.Application.Interfaces;
using MediatR;

namespace DistributedConfigHub.Application.Features.Commands;

public record UpdateConfigurationCommand(Guid Id, string Value, string CallerApplicationName = "") : IRequest<bool>;

public class UpdateConfigurationCommandHandler(
    IConfigurationRepository repository, 
    IMessagePublisher messagePublisher) 
    : IRequestHandler<UpdateConfigurationCommand, bool>
{
    public async Task<bool> Handle(UpdateConfigurationCommand request, CancellationToken cancellationToken)
    {
        var record = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (record is null) 
            throw new KeyNotFoundException($"Configuration with Id {request.Id} not found.");

        if (!string.Equals(record.ApplicationName, request.CallerApplicationName, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Security Violation: Unauthorized access attempt!");
        
        record.UpdateValue(request.Value, "admin");
        await repository.UpdateAsync(record, cancellationToken);
        
        await messagePublisher.PublishConfigurationUpdatedEventAsync(record.ApplicationName, record.Environment, cancellationToken);

        return true;
    }
}