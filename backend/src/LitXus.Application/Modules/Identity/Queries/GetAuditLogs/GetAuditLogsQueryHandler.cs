using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Identity.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Identity.Queries.GetAuditLogs;

public class GetAuditLogsQueryHandler(IAppDbContext db, IIdentityUserService identityUserService)
    : IRequestHandler<GetAuditLogsQuery, IReadOnlyList<AuditLogDto>>
{
    // Not yet paginated (Phase 1 scope) — capped at the most recent 200 matching rows so a
    // growing audit table can't return an unbounded payload. Add real pagination if/when the
    // Admin UI needs to page through more history than this.
    private const int MaxResults = 200;

    public async Task<IReadOnlyList<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EntityName))
            query = query.Where(a => a.EntityName == request.EntityName);
        if (!string.IsNullOrWhiteSpace(request.EntityId))
            query = query.Where(a => a.EntityId == request.EntityId);
        if (request.UserId.HasValue)
            query = query.Where(a => a.UserId == request.UserId);
        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(a => a.Action == request.Action);
        if (request.DateFromUtc.HasValue)
            query = query.Where(a => a.TimestampUtc >= request.DateFromUtc);
        if (request.DateToUtc.HasValue)
            query = query.Where(a => a.TimestampUtc <= request.DateToUtc);

        var logs = await query
            .OrderByDescending(a => a.TimestampUtc)
            .Take(MaxResults)
            .ToListAsync(cancellationToken);

        var users = await identityUserService.GetUsersAsync(cancellationToken);
        var emailById = users.ToDictionary(u => u.Id, u => u.Email);

        return logs.Select(a => new AuditLogDto(
            a.Id,
            a.EntityName,
            a.EntityId,
            a.Action,
            a.BeforeValuesJson,
            a.AfterValuesJson,
            a.Reason,
            a.UserId,
            a.UserId.HasValue && emailById.TryGetValue(a.UserId.Value, out var email) ? email : null,
            a.IpAddress,
            a.TimestampUtc)).ToList();
    }
}
