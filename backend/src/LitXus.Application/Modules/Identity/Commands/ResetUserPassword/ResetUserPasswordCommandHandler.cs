using LitXus.Application.Common.Interfaces;
using MediatR;

namespace LitXus.Application.Modules.Identity.Commands.ResetUserPassword;

public class ResetUserPasswordCommandHandler(IIdentityUserService identityUserService) : IRequestHandler<ResetUserPasswordCommand>
{
    public Task Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken) =>
        identityUserService.ResetUserPasswordAsync(request.UserId, request.NewPassword, cancellationToken);
}
