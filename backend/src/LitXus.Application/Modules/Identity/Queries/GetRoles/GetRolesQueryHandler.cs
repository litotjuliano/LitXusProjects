using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Identity.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Identity.Queries.GetRoles;

public class GetRolesQueryHandler(IAppDbContext db) : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    public async Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await db.AppRoles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(cancellationToken);

        var rolePermissionCodes = await (
            from rp in db.RolePermissions.AsNoTracking()
            join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
            select new { rp.RoleId, p.Code }
        ).ToListAsync(cancellationToken);

        return roles.Select(r => new RoleDto(
            r.Id,
            r.Name,
            r.Description,
            rolePermissionCodes.Where(rp => rp.RoleId == r.Id).Select(rp => rp.Code).OrderBy(c => c).ToList())
        ).ToList();
    }
}
