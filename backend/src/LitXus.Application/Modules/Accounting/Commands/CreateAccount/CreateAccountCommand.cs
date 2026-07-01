using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Commands.CreateAccount;

public record CreateAccountCommand(
    string Code,
    string Name,
    string Type,
    Guid? ParentAccountId) : IRequest<AccountDto>;
