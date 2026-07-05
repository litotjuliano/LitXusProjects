using LitXus.Application.Modules.Sales.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Sales.Queries.GetCreditNoteById;

public record GetCreditNoteByIdQuery(Guid Id) : IRequest<CreditNoteDto>;
