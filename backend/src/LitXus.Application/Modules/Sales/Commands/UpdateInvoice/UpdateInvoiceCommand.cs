using LitXus.Application.Modules.Sales.Commands.CreateInvoice;
using LitXus.Application.Modules.Sales.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Sales.Commands.UpdateInvoice;

public record UpdateInvoiceCommand(
    Guid Id,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    string? Notes,
    IReadOnlyList<InvoiceLineInput> Lines) : IRequest<InvoiceDto>;
