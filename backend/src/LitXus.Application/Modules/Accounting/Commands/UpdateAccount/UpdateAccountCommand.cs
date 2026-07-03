using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Commands.UpdateAccount;

public record UpdateAccountCommand(Guid Id, string Name, Guid? ParentAccountId) : IRequest<AccountDto>;
