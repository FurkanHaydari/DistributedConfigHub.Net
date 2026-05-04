using DistributedConfigHub.Application.Interfaces;
using DistributedConfigHub.Domain.Entities;
using DistributedConfigHub.Domain.Enums;
using MediatR;

namespace DistributedConfigHub.Application.Features.Commands;

public record CreateConfigurationCommand(
    string Name, 
    ConfigurationType Type, 
    string Value, 
    string ApplicationName, 
    string Environment,
    string CallerApplicationName = ""
) : IRequest<Guid>, ITenantIsolatedRequest;

public class CreateConfigurationCommandHandler(
    IConfigurationRepository repository, 
    IMessagePublisher messagePublisher) 
    : IRequestHandler<CreateConfigurationCommand, Guid>
{
    public async Task<Guid> Handle(CreateConfigurationCommand request, CancellationToken cancellationToken)
    {
        // 1. Does such a record exist in the database (Active or Passive):
        var existingRecord = await repository.GetByNameAsync(request.Name, request.ApplicationName, request.Environment, cancellationToken);

        if (existingRecord != null)
        {
            if (existingRecord.IsActive)
            {
                // Record exists and is already active. Conflict (409 Conflict)
                throw new InvalidOperationException($"Configuration '{request.Name}' already exists.");
            }
            else
            {
                // Record exists but is passive (Soft-deleted)
                // Do not allow changing the Type, because old logs and Consumers might crash.
                if (existingRecord.Type != request.Type)
                    throw new InvalidOperationException($"A deleted configuration exists but with type '{existingRecord.Type}'. You cannot change the type of a restored configuration.");

                // To prevent Unique Index (DbUpdateException) error and keep the Audit Log chain intact
                // instead of adding a new row, we resurrect (Restore) the old soft-deleted record and update it.
                existingRecord.Activate("admin");
                existingRecord.UpdateValue(request.Value, "admin");
                
                await repository.UpdateAsync(existingRecord, cancellationToken);
                
                // Dispatch Event
                await messagePublisher.PublishConfigurationUpdatedEventAsync(existingRecord.ApplicationName, existingRecord.Environment, cancellationToken);
                
                return existingRecord.Id;
            }
        }

        // 2. If the record does not exist at all, create it from scratch
        var newRecord = new ConfigurationRecord(
            request.Name, 
            request.Type, 
            request.Value, 
            request.ApplicationName, 
            request.Environment,
            "admin");

        // 3. Kaydet ve haber ver
        await repository.AddAsync(newRecord, cancellationToken);
        await messagePublisher.PublishConfigurationUpdatedEventAsync(newRecord.ApplicationName, newRecord.Environment, cancellationToken);

        return newRecord.Id;
    }
}