using MediatR;

namespace LitXus.Application.Modules.Accounting.Commands.ImportBankStatementLines;

public record ImportBankStatementLinesCommand(Guid BankAccountId, string CsvContent) : IRequest<int>;
