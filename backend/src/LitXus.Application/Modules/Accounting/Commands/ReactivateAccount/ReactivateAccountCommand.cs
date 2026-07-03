using MediatR;

namespace LitXus.Application.Modules.Accounting.Commands.ReactivateAccount;

public record ReactivateAccountCommand(Guid Id) : IRequest;
