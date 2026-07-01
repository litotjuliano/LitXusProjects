using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using LitXus.Domain.Modules.Accounting.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.Commands.VoidGLEntry;

public class VoidGLEntryCommandHandler(IAppDbContext db, IAuditLogger auditLogger)
    : IRequestHandler<VoidGLEntryCommand, GLEntryDto>
{
    public async Task<GLEntryDto> Handle(VoidGLEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await db.GLEntries
            .Include(e => e.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(e => e.Id == request.GLEntryId, cancellationToken)
            ?? throw new NotFoundException(nameof(GLEntry), request.GLEntryId);

        var before = new { entry.Status };
        entry.Void(request.Reason);

        await auditLogger.LogAsync(
            nameof(GLEntry), entry.Id.ToString(), "Void",
            before, new { entry.Status }, request.Reason, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return entry.ToDto();
    }
}
