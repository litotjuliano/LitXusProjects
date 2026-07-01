using LitXus.Application.Modules.Identity.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Identity.Queries.GetPermissions;

public record GetPermissionsQuery : IRequest<IReadOnlyList<PermissionDto>>;
