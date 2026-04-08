using DistributedConfigHub.Application.Interfaces;

namespace DistributedConfigHub.Infrastructure.Data.Interceptors;

public class AuditContextAccessor : IAuditContextAccessor
{
    private static readonly AsyncLocal<AuditContext?> _currentContext = new();

    public AuditContext? Current => _currentContext.Value;

    public void SetContext(string action, string reason)
    {
        _currentContext.Value = new AuditContext(action, reason);
    }
}
