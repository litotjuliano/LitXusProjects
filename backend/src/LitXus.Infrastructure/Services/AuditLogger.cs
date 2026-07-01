using System.Text.Json;
using LitXus.Application.Common.Interfaces;
using LitXus.Domain.Modules.Shared.Entities;

namespace LitXus.Infrastructure.Services;

public class AuditLogger(IAppDbContext db, ICurrentUserService currentUserService, IDateTimeProvider dateTimeProvider) : IAuditLogger
{
    public Task LogAsync(
        string entityName,
        string entityId,
        string action,
        object? before,
        object? after,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            BeforeValuesJson = before is null ? null : JsonSerializer.Serialize(before),
            AfterValuesJson = after is null ? null : JsonSerializer.Serialize(after),
            Reason = reason,
            UserId = currentUserService.UserId,
            IpAddress = currentUserService.IpAddress,
            UserAgent = currentUserService.UserAgent,
            TimestampUtc = dateTimeProvider.UtcNow,
        });

        // Not saved here — the caller's SaveChangesAsync (already part of the same command
        // handler's unit of work) persists this row alongside the entity change it describes.
        return Task.CompletedTask;
    }
}
