using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Domain.Modules.Identity.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Identity.Commands.AssignRole;

public class AssignRoleCommandHandler(IAppDbContext db, IAuditLogger auditLogger) : IRequestHandler<AssignRoleCommand>
{
    public async Task Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await db.AppRoles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);

        // Super Admin is provisioned only via seeding, never through the general user-role-assignment
        // endpoint — otherwise any Admin (who holds Admin.Users.Update) could grant themselves or
        // anyone else full install-owner access.
        if (role.Name == "Super Admin")
        {
            throw new ForbiddenException("The Super Admin role cannot be assigned through this endpoint.");
        }

        var alreadyAssigned = await db.AppUserRoles
            .AnyAsync(ur => ur.UserId == request.UserId && ur.RoleId == request.RoleId, cancellationToken);
        if (alreadyAssigned)
        {
            return;
        }

        db.AppUserRoles.Add(UserRole.Create(request.UserId, request.RoleId));

        await auditLogger.LogAsync(
            "UserRole", request.UserId.ToString(), "AssignRole",
            null, new { request.RoleId }, null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
