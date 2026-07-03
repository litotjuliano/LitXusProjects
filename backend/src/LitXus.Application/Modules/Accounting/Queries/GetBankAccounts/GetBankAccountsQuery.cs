using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Queries.GetBankAccounts;

public record GetBankAccountsQuery : IRequest<IReadOnlyList<BankAccountDto>>;
