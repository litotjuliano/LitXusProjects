using LitXus.Application.Modules.Sales.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Sales.Commands.VoidInvoice;

public record VoidInvoiceCommand(Guid InvoiceId, string Reason) : IRequest<InvoiceDto>;
