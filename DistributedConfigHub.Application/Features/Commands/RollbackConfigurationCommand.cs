using System.Text.Json;
using DistributedConfigHub.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace DistributedConfigHub.Application.Features.Commands;

public record RollbackConfigurationCommand(Guid Id, Guid AuditLogId) : IRequest<bool>;

public class RollbackConfigurationCommandHandler(IConfigurationRepository repository, IAuditLogRepository auditLogRepository, IAuditContextAccessor auditContextAccessor, IMessagePublisher messagePublisher) : IRequestHandler<RollbackConfigurationCommand, bool>
{
    public async Task<bool> Handle(RollbackConfigurationCommand request, CancellationToken cancellationToken)
    {
        var record = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (record is null) return false;

        var auditLog = await auditLogRepository.GetByIdAsync(request.AuditLogId, cancellationToken);
        if (auditLog is null || auditLog.EntityId != request.Id) return false;

        if (string.IsNullOrEmpty(auditLog.OldValues)) return false;

        var jsonDoc = JsonDocument.Parse(auditLog.OldValues);
        if (jsonDoc.RootElement.TryGetProperty("Value", out var valueElement))
        {
            var oldStringValue = valueElement.GetString();
            if (oldStringValue != null)
            {
                record.UpdateValue(oldStringValue);
                
                // Infrastructure katmanına context aktarımı
                auditContextAccessor.SetContext("ROLLBACK", $"Reverted from Audit Log ID: {request.AuditLogId}");
                
                await repository.UpdateAsync(record, cancellationToken);

                await messagePublisher.PublishConfigurationUpdatedEventAsync(record.ApplicationName, record.Environment, cancellationToken);
                return true;
            }
        }

        return false;
    }
}
