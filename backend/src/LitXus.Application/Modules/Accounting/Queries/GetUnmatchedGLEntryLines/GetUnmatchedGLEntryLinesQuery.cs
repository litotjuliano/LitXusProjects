using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Queries.GetUnmatchedGLEntryLines;

public record GetUnmatchedGLEntryLinesQuery(Guid BankAccountId) : IRequest<IReadOnlyList<UnmatchedGLEntryLineDto>>;
