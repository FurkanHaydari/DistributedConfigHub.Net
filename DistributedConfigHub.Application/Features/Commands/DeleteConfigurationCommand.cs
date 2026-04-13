using DistributedConfigHub.Application.Interfaces;
using MediatR;

namespace DistributedConfigHub.Application.Features.Commands;

public record DeleteConfigurationCommand(Guid Id) : IRequest<bool>;

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

        record.Deactivate("admin");
        await repository.UpdateAsync(record, cancellationToken);

        await messagePublisher.PublishConfigurationUpdatedEventAsync(record.ApplicationName, record.Environment, cancellationToken);

        return true;
    }
}