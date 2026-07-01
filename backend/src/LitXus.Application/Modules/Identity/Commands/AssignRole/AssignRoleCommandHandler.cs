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
        var roleExists = await db.AppRoles.AnyAsync(r => r.Id == request.RoleId, cancellationToken);
        if (!roleExists)
        {
            throw new NotFoundException(nameof(Role), request.RoleId);
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
