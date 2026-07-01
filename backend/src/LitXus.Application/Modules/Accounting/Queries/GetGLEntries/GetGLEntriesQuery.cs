using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Queries.GetGLEntries;

public record GetGLEntriesQuery(string? Status = null) : IRequest<IReadOnlyList<GLEntryDto>>;
