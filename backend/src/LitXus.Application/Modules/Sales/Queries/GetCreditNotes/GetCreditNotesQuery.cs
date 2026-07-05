using LitXus.Application.Modules.Sales.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Sales.Queries.GetCreditNotes;

public record GetCreditNotesQuery : IRequest<IReadOnlyList<CreditNoteDto>>;
