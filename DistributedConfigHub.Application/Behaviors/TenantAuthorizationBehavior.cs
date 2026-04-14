using DistributedConfigHub.Application.Interfaces;
using MediatR;

namespace DistributedConfigHub.Application.Behaviors;

public class TenantAuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITenantIsolatedRequest
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.ApplicationName, request.CallerApplicationName, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Güvenlik İhlali: {request.CallerApplicationName} servisi, {request.ApplicationName} adına işlem yapamaz!");

        return await next();
    }
}