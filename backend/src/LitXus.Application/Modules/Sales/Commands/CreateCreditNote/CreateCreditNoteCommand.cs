using LitXus.Application.Modules.Sales.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Sales.Commands.CreateCreditNote;

public record CreateCreditNoteCommand(Guid InvoiceId, string Reason, decimal Amount) : IRequest<CreditNoteDto>;
