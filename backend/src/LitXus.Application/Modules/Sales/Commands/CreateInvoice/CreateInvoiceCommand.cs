using LitXus.Application.Modules.Sales.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Sales.Commands.CreateInvoice;

public record InvoiceLineInput(string Description, decimal Quantity, string? UnitOfMeasure, decimal UnitPrice, Guid? TaxCodeId);

public record CreateInvoiceCommand(
    Guid CustomerId,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    string? Notes,
    IReadOnlyList<InvoiceLineInput> Lines) : IRequest<InvoiceDto>;
