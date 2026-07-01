using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Identity.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Identity.Queries.GetPermissions;

public class GetPermissionsQueryHandler(IAppDbContext db) : IRequestHandler<GetPermissionsQuery, IReadOnlyList<PermissionDto>>
{
    public async Task<IReadOnlyList<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await db.Permissions.AsNoTracking()
            .OrderBy(p => p.Module).ThenBy(p => p.Entity).ThenBy(p => p.Operation)
            .ToListAsync(cancellationToken);

        return permissions.Select(p => new PermissionDto(p.Id, p.Module, p.Entity, p.Operation, p.Code)).ToList();
    }
}
