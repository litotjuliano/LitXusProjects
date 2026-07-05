using LitXus.Application.Modules.Sales.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Sales.Commands.IssueInvoice;

public record IssueInvoiceCommand(Guid InvoiceId) : IRequest<InvoiceDto>;
