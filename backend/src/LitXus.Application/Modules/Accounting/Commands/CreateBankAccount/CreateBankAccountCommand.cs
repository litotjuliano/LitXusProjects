using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Commands.CreateBankAccount;

public record CreateBankAccountCommand(Guid AccountId, string BankName, string AccountNumber) : IRequest<BankAccountDto>;
