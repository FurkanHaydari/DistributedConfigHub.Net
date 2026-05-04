using System.Text.Json;
using DistributedConfigHub.Application.Interfaces;
using MediatR;

namespace DistributedConfigHub.Application.Features.Commands;

public record RollbackConfigurationCommand(Guid Id, Guid AuditLogId, string CallerApplicationName = "") : IRequest<bool>;

public class RollbackConfigurationCommandHandler(
    IConfigurationRepository repository, 
    IAuditLogRepository auditLogRepository, 
    IAuditContextAccessor auditContextAccessor, 
    IMessagePublisher messagePublisher) 
    : IRequestHandler<RollbackConfigurationCommand, bool>
{
    public async Task<bool> Handle(RollbackConfigurationCommand request, CancellationToken cancellationToken)
    {
        var record = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (record is null) 
            throw new KeyNotFoundException($"Configuration with Id {request.Id} not found.");

        var auditLog = await auditLogRepository.GetByIdAsync(request.AuditLogId, cancellationToken);
        if (auditLog is null || auditLog.EntityId != request.Id) 
            throw new InvalidOperationException("Invalid Audit Log record for this configuration.");

        if (string.IsNullOrEmpty(auditLog.OldValues)) 
            throw new InvalidOperationException("No previous values found to rollback.");

        if (!string.Equals(record.ApplicationName, request.CallerApplicationName, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Security Violation: Unauthorized access attempt!");

        var jsonDoc = JsonDocument.Parse(auditLog.OldValues);
        bool hasChanges = false;
        string rolledBackBy = "system-rollback";

        if (jsonDoc.RootElement.TryGetProperty("Value", out var valueElement))
        {
            var oldStringValue = valueElement.GetString();
            if (oldStringValue != null && oldStringValue != record.Value)
            {
                record.UpdateValue(oldStringValue, rolledBackBy);
                hasChanges = true;
            }
        }

        if (jsonDoc.RootElement.TryGetProperty("IsActive", out var activeElement))
        {
            var oldActiveValue = activeElement.GetBoolean();
            if (oldActiveValue != record.IsActive)
            {
                if (oldActiveValue) record.Activate(rolledBackBy); 
                else record.Deactivate(rolledBackBy);
                
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            // Pass context to Infrastructure layer
            auditContextAccessor.SetContext("ROLLBACK", $"Reverted from Audit Log ID: {request.AuditLogId}");
            
            await repository.UpdateAsync(record, cancellationToken);
            await messagePublisher.PublishConfigurationUpdatedEventAsync(record.ApplicationName, record.Environment, cancellationToken);
            
            return true;
        }

        return false; 
    }
}