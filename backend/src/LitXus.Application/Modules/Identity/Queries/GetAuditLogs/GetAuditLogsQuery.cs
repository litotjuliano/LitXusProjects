using LitXus.Application.Modules.Identity.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Identity.Queries.GetAuditLogs;

public record GetAuditLogsQuery(
    string? EntityName = null,
    string? EntityId = null,
    Guid? UserId = null,
    string? Action = null,
    DateTime? DateFromUtc = null,
    DateTime? DateToUtc = null) : IRequest<IReadOnlyList<AuditLogDto>>;
