using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Queries.GetAccounts;

public record GetAccountsQuery(string? Type = null, bool IncludeInactive = false) : IRequest<IReadOnlyList<AccountDto>>;
