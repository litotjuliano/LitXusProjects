using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Identity.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Identity.Commands.CreateUser;

public class CreateUserCommandHandler(IIdentityUserService identityUserService) : IRequestHandler<CreateUserCommand, UserSummaryDto>
{
    public Task<UserSummaryDto> Handle(CreateUserCommand request, CancellationToken cancellationToken) =>
        identityUserService.CreateUserAsync(request.Email, request.FullName, request.Password, request.RoleId, cancellationToken);
}
