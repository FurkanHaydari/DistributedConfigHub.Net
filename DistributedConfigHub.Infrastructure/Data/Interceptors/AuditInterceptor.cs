using System.Text.Json;
using DistributedConfigHub.Domain.Entities;
using DistributedConfigHub.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DistributedConfigHub.Infrastructure.Data.Interceptors;

public sealed class AuditInterceptor(IAuditContextAccessor auditContextAccessor) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var auditEntries = new List<AuditLog>();
        var currentContext = auditContextAccessor.Current;

        foreach (var entry in context.ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
            }
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
            }

            if (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
            {
                var mappedAction = entry.State switch
                {
                    EntityState.Added => "INSERT",
                    EntityState.Modified => "UPDATE",
                    EntityState.Deleted => "DELETE",
                    _ => "UNKNOWN"
                };

                var auditLog = new AuditLog
                {
                    EntityName = entry.Metadata.Name.Split('.').Last(),
                    Action = currentContext?.Action ?? mappedAction
                };
                
                if (currentContext?.Reason != null)
                {
                    auditLog.Reason = currentContext.Reason;
                }

                var idProperty = entry.Property("Id");
                if (idProperty != null && idProperty.CurrentValue != null)
                {
                    auditLog.EntityId = (Guid)idProperty.CurrentValue;
                }

                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();
                var affectedColumns = new List<string>();

                foreach (var property in entry.Properties)
                {
                    string propertyName = property.Metadata.Name;

                    if (entry.State == EntityState.Added)
                    {
                        newValues[propertyName] = property.CurrentValue;
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        oldValues[propertyName] = property.OriginalValue;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        if (property.IsModified && propertyName != "UpdatedAt")
                        {
                            oldValues[propertyName] = property.OriginalValue;
                            newValues[propertyName] = property.CurrentValue;
                            affectedColumns.Add(propertyName);
                        }
                    }
                }

                if (oldValues.Count > 0) auditLog.OldValues = JsonSerializer.Serialize(oldValues);
                if (newValues.Count > 0) auditLog.NewValues = JsonSerializer.Serialize(newValues);
                if (affectedColumns.Count > 0) auditLog.AffectedColumns = JsonSerializer.Serialize(affectedColumns);

                // Only save meaningful modifications (Skip Modified if the field hasn't changed)
                if (entry.State == EntityState.Modified && affectedColumns.Count == 0) continue;

                auditEntries.Add(auditLog);
            }
        }

        if (auditEntries.Any())
        {
            context.Set<AuditLog>().AddRange(auditEntries);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
