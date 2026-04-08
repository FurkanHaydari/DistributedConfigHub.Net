using DistributedConfigHub.Domain.Entities;

namespace DistributedConfigHub.Application.Interfaces;

public interface IAuditLogRepository
{
    Task<IEnumerable<AuditLog>> GetHistoryAsync(Guid entityId, CancellationToken cancellationToken = default);
    Task<AuditLog?> GetByIdAsync(Guid auditLogId, CancellationToken cancellationToken = default);
}
