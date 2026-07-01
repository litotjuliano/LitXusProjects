using LitXus.Application.Common.Interfaces;
using MediatR;

namespace LitXus.Application.Modules.Identity.Commands.UpdateUserStatus;

public class UpdateUserStatusCommandHandler(
    IIdentityUserService identityUserService,
    IAuditLogger auditLogger,
    IAppDbContext db) : IRequestHandler<UpdateUserStatusCommand>
{
    public async Task Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
    {
        // SetUserActiveAsync saves via UserManager immediately (it shares the same underlying
        // DbContext as IAppDbContext), so the audit entry added below needs its own explicit
        // SaveChangesAsync — it won't ride along with a save that already happened.
        await identityUserService.SetUserActiveAsync(request.UserId, request.IsActive, cancellationToken);

        await auditLogger.LogAsync(
            "User", request.UserId.ToString(),
            request.IsActive ? "Activate" : "Deactivate",
            null, new { request.IsActive }, null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
