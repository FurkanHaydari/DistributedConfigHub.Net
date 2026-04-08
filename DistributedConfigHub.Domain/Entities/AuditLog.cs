namespace DistributedConfigHub.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // INSERT, UPDATE, DELETE
    
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? AffectedColumns { get; set; }
    
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserIp { get; set; }
    
    public string? Reason { get; set; } // İşlemin özel nedeni varsa (örneğin: ROLLBACK to xxx)
    
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
