using LitXus.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Identity.Commands.RevokeRole;

public class RevokeRoleCommandHandler(IAppDbContext db, IAuditLogger auditLogger) : IRequestHandler<RevokeRoleCommand>
{
    public async Task Handle(RevokeRoleCommand request, CancellationToken cancellationToken)
    {
        var userRole = await db.AppUserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == request.RoleId, cancellationToken);

        if (userRole is null)
        {
            return;
        }

        db.AppUserRoles.Remove(userRole);

        await auditLogger.LogAsync(
            "UserRole", request.UserId.ToString(), "RevokeRole",
            new { request.RoleId }, null, null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
