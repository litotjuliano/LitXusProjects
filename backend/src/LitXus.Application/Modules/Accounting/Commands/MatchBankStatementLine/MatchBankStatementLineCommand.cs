using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Commands.MatchBankStatementLine;

public record MatchBankStatementLineCommand(Guid StatementLineId, Guid GLEntryLineId) : IRequest<BankStatementLineDto>;
