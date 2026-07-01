using MediatR;

namespace LitXus.Application.Modules.Identity.Commands.AssignRole;

public record AssignRoleCommand(Guid UserId, Guid RoleId) : IRequest;
