using DistributedConfigHub.Application.Interfaces;
using DistributedConfigHub.Domain.Entities;
using MediatR;

namespace DistributedConfigHub.Application.Features.Queries;

public record GetConfigurationHistoryQuery(Guid Id, string CallerApplicationName = "") : IRequest<IEnumerable<AuditLog>>;

public class GetConfigurationHistoryQueryHandler(
    IAuditLogRepository auditLogRepository, 
    IConfigurationRepository configurationRepository)
    : IRequestHandler<GetConfigurationHistoryQuery, IEnumerable<AuditLog>>
{
    public async Task<IEnumerable<AuditLog>> Handle(GetConfigurationHistoryQuery request, CancellationToken cancellationToken)
    {
        var record = await configurationRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (record is null) throw new KeyNotFoundException($"Configuration with Id {request.Id} not found.");

        if (!string.Equals(record.ApplicationName, request.CallerApplicationName, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Security Violation: Service '{request.CallerApplicationName}' cannot read history logs of service '{record.ApplicationName}'!");

        return await auditLogRepository.GetHistoryAsync(request.Id, cancellationToken);
    }
}