using LitXus.Application.Modules.Identity.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Identity.Queries.GetRoles;

public record GetRolesQuery : IRequest<IReadOnlyList<RoleDto>>;
