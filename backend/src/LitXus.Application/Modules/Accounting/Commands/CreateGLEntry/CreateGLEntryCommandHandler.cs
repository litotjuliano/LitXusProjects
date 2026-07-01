using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using LitXus.Domain.Modules.Accounting.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.Commands.CreateGLEntry;

public class CreateGLEntryCommandHandler(IAppDbContext db) : IRequestHandler<CreateGLEntryCommand, GLEntryDto>
{
    public async Task<GLEntryDto> Handle(CreateGLEntryCommand request, CancellationToken cancellationToken)
    {
        var accountIds = request.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await db.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        var lines = request.Lines.Select(l =>
        {
            if (!accounts.TryGetValue(l.AccountId, out var account))
            {
                throw new NotFoundException(nameof(Account), l.AccountId);
            }

            return GLEntryLine.Create(account, l.DebitAmount, l.CreditAmount, l.LineDescription);
        }).ToList();

        var entry = GLEntry.CreateDraft(request.EntryDate, request.Description, lines);

        db.GLEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);

        return entry.ToDto();
    }
}
