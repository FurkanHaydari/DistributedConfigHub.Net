using DistributedConfigHub.Application.Interfaces;
using DistributedConfigHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DistributedConfigHub.Infrastructure.Data.Repositories;

public class AuditLogRepository(ConfigDbContext dbContext) : IAuditLogRepository
{
    public async Task<IEnumerable<AuditLog>> GetHistoryAsync(Guid entityId, CancellationToken cancellationToken = default)
    {
        return await dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<AuditLog?> GetByIdAsync(Guid auditLogId, CancellationToken cancellationToken = default)
    {
        return await dbContext.AuditLogs.FirstOrDefaultAsync(x => x.Id == auditLogId, cancellationToken);
    }
}
