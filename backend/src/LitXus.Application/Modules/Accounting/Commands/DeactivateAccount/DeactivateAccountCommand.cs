using MediatR;

namespace LitXus.Application.Modules.Accounting.Commands.DeactivateAccount;

public record DeactivateAccountCommand(Guid Id) : IRequest;
