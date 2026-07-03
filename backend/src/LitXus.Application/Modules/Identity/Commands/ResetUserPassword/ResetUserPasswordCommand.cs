using MediatR;

namespace LitXus.Application.Modules.Identity.Commands.ResetUserPassword;

public record ResetUserPasswordCommand(Guid UserId, string NewPassword) : IRequest;
