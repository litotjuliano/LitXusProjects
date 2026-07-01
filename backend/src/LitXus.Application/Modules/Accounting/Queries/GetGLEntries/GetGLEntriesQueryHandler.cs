using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using LitXus.Domain.Modules.Accounting.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Application.Modules.Accounting.Queries.GetGLEntries;

public class GetGLEntriesQueryHandler(IAppDbContext db) : IRequestHandler<GetGLEntriesQuery, IReadOnlyList<GLEntryDto>>
{
    public async Task<IReadOnlyList<GLEntryDto>> Handle(GetGLEntriesQuery request, CancellationToken cancellationToken)
    {
        var query = db.GLEntries.AsNoTracking().Include(e => e.Lines).ThenInclude(l => l.Account).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<GLEntryStatus>(request.Status, out var status))
        {
            query = query.Where(e => e.Status == status);
        }

        var entries = await query.OrderByDescending(e => e.EntryDate).ToListAsync(cancellationToken);
        return entries.Select(e => e.ToDto()).ToList();
    }
}
