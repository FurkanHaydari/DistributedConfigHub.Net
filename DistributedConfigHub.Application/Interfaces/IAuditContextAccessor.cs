namespace DistributedConfigHub.Application.Interfaces;

public interface IAuditContextAccessor
{
    AuditContext? Current { get; }
    void SetContext(string action, string reason);
}
