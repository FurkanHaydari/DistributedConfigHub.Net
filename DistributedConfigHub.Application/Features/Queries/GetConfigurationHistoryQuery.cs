using DistributedConfigHub.Application.Interfaces;
using DistributedConfigHub.Domain.Entities;
using FluentValidation;
using MediatR;

namespace DistributedConfigHub.Application.Features.Queries;

public record GetConfigurationHistoryQuery(Guid Id) : IRequest<IEnumerable<AuditLog>>;

public class GetConfigurationHistoryQueryHandler(IAuditLogRepository auditLogRepository) : IRequestHandler<GetConfigurationHistoryQuery, IEnumerable<AuditLog>>
{
    public async Task<IEnumerable<AuditLog>> Handle(GetConfigurationHistoryQuery request, CancellationToken cancellationToken)
    {
        return await auditLogRepository.GetHistoryAsync(request.Id, cancellationToken);
    }
}
