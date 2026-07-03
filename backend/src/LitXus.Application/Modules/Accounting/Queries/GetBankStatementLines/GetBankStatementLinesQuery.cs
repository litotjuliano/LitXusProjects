using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Queries.GetBankStatementLines;

public record GetBankStatementLinesQuery(Guid BankAccountId) : IRequest<IReadOnlyList<BankStatementLineDto>>;
