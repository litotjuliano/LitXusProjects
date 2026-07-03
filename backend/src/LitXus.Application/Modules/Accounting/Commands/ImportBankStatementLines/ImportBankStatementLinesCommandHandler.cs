using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Services;
using LitXus.Domain.Modules.Accounting.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.Commands.ImportBankStatementLines;

public class ImportBankStatementLinesCommandHandler(IAppDbContext db) : IRequestHandler<ImportBankStatementLinesCommand, int>
{
    public async Task<int> Handle(ImportBankStatementLinesCommand request, CancellationToken cancellationToken)
    {
        var bankAccountExists = await db.BankAccounts.AnyAsync(b => b.Id == request.BankAccountId, cancellationToken);
        if (!bankAccountExists)
        {
            throw new NotFoundException(nameof(BankAccount), request.BankAccountId);
        }

        var parsedLines = BankStatementCsvParser.Parse(request.CsvContent);

        var lines = parsedLines
            .Select(p => BankStatementLine.Create(request.BankAccountId, p.TransactionDate, p.Description, p.Amount))
            .ToList();

        db.BankStatementLines.AddRange(lines);
        await db.SaveChangesAsync(cancellationToken);

        return lines.Count;
    }
}
