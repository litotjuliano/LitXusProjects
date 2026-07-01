using System.Text.Json;
using LitXus.Application.Common.Interfaces;
using LitXus.Domain.Common;
using LitXus.Domain.Modules.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LitXus.Infrastructure.Persistence.Interceptors;

/// <summary>See docs/07_Audit_Trail.md §7.3 — centralizes audit capture so handlers don't have to
/// remember to log every change by hand. Explicit, semantically-named actions (e.g. "Approve" for
/// posting a GL entry) are still logged separately by IAuditLogger from within command handlers.</summary>
public class AuditSaveChangesInterceptor(ICurrentUserService currentUserService, IDateTimeProvider dateTimeProvider)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        StampTimestamps(eventData.Context);
        CaptureAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        StampTimestamps(eventData.Context);
        CaptureAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>Sets CreatedAtUtc/CreatedBy on insert and ModifiedAtUtc/ModifiedBy on update for every
    /// BaseEntity — not just IAuditable ones, since these columns exist on all of them (docs/02_Database_Schema.md).</summary>
    private void StampTimestamps(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = dateTimeProvider.UtcNow;
                    entry.Entity.CreatedBy = currentUserService.UserId;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedAtUtc = dateTimeProvider.UtcNow;
                    entry.Entity.ModifiedBy = currentUserService.UserId;
                    break;
            }
        }
    }

    private void CaptureAuditEntries(DbContext? context)
    {
        if (context is null) return;

        var auditEntries = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var entityId = entry.Property("Id").CurrentValue?.ToString() ?? string.Empty;

            auditEntries.Add(new AuditLog
            {
                EntityName = entry.Entity.GetType().Name,
                EntityId = entityId,
                Action = entry.State switch
                {
                    EntityState.Added => "Create",
                    EntityState.Deleted => "Delete",
                    _ => "Update",
                },
                BeforeValuesJson = entry.State != EntityState.Added ? Serialize(entry.OriginalValues) : null,
                AfterValuesJson = entry.State != EntityState.Deleted ? Serialize(entry.CurrentValues) : null,
                UserId = currentUserService.UserId,
                IpAddress = currentUserService.IpAddress,
                UserAgent = currentUserService.UserAgent,
                TimestampUtc = dateTimeProvider.UtcNow,
            });
        }

        if (auditEntries.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditEntries);
        }
    }

    private static string Serialize(PropertyValues values)
    {
        var dict = values.Properties.ToDictionary(p => p.Name, p => values[p]);
        return JsonSerializer.Serialize(dict);
    }
}
