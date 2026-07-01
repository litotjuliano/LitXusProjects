using MediatR;

namespace LitXus.Application.Modules.Identity.Commands.UpdateUserStatus;

public record UpdateUserStatusCommand(Guid UserId, bool IsActive) : IRequest;
