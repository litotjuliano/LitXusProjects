using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using LitXus.Domain.Modules.Accounting.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.Commands.UpdateGLEntry;

public class UpdateGLEntryCommandHandler(IAppDbContext db) : IRequestHandler<UpdateGLEntryCommand, GLEntryDto>
{
    public async Task<GLEntryDto> Handle(UpdateGLEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await db.GLEntries
            .Include(e => e.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(GLEntry), request.Id);

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

        entry.UpdateLines(request.EntryDate, request.Description, lines);

        // GLEntryLine.Id is a client-generated GUID (BaseEntity's Guid.CreateVersion7() default),
        // not a database-generated value — so when these new lines are added purely via the
        // `entry.Lines` navigation collection (not db.Add()), EF Core's change tracker can't tell
        // them apart from an existing row and infers Modified instead of Added, generating UPDATEs
        // that affect 0 rows (DbUpdateConcurrencyException). Explicitly (re-)adding them to the
        // DbSet forces the correct Added state. entry.UpdateLines(...) is a Create-style graph
        // change, so this doesn't apply to CreateGLEntryCommandHandler — there, db.GLEntries.Add(entry)
        // marks the whole reachable graph Added unconditionally, so this ambiguity never arises.
        db.GLEntryLines.AddRange(lines);

        await db.SaveChangesAsync(cancellationToken);

        return entry.ToDto();
    }
}
