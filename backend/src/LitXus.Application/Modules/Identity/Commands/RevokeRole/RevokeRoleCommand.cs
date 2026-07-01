using MediatR;

namespace LitXus.Application.Modules.Identity.Commands.RevokeRole;

public record RevokeRoleCommand(Guid UserId, Guid RoleId) : IRequest;
