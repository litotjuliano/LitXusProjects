using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using LitXus.Domain.Modules.Accounting.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.Commands.PostGLEntry;

public class PostGLEntryCommandHandler(
    IAppDbContext db,
    INumberSequenceGenerator numberSequenceGenerator,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditLogger auditLogger) : IRequestHandler<PostGLEntryCommand, GLEntryDto>
{
    public async Task<GLEntryDto> Handle(PostGLEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await db.GLEntries
            .Include(e => e.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(e => e.Id == request.GLEntryId, cancellationToken)
            ?? throw new NotFoundException(nameof(GLEntry), request.GLEntryId);

        var before = new { entry.Status };
        var entryNumber = await numberSequenceGenerator.NextGLEntryNumberAsync(cancellationToken);

        entry.Post(entryNumber, currentUserService.UserId ?? Guid.Empty, dateTimeProvider.UtcNow);

        await auditLogger.LogAsync(
            nameof(GLEntry), entry.Id.ToString(), "Approve",
            before, new { entry.Status, entry.EntryNumber }, null, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return entry.ToDto();
    }
}
