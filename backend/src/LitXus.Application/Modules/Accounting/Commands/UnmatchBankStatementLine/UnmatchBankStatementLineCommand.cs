using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Commands.UnmatchBankStatementLine;

public record UnmatchBankStatementLineCommand(Guid StatementLineId) : IRequest<BankStatementLineDto>;
