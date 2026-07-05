using LitXus.Domain.Modules.Sales.Entities;

namespace LitXus.Application.Modules.Sales.Dtos;

public record InvoiceLineDto(
    Guid Id,
    string Description,
    decimal Quantity,
    string? UnitOfMeasure,
    decimal UnitPrice,
    decimal LineTotal,
    Guid? TaxCodeId,
    string? TaxCodeName);

public record InvoiceDto(
    Guid Id,
    string? InvoiceNumber,
    Guid CustomerId,
    string CustomerCode,
    string CustomerName,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    string Status,
    bool IsOverdue,
    decimal SubTotal,
    decimal SSTAmount,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal OutstandingBalance,
    string? Notes,
    string? VoidReason,
    IReadOnlyList<InvoiceLineDto> Lines);

public static class InvoiceMappingExtensions
{
    public static InvoiceLineDto ToDto(this InvoiceLine line) => new(
        line.Id, line.Description, line.Quantity, line.UnitOfMeasure, line.UnitPrice, line.LineTotal, line.TaxCodeId, line.TaxCode?.Name);

    public static InvoiceDto ToDto(this Invoice invoice, Customer customer, DateOnly today) => new(
        invoice.Id,
        invoice.InvoiceNumber,
        invoice.CustomerId,
        customer.Code,
        customer.CompanyName,
        invoice.InvoiceDate,
        invoice.DueDate,
        invoice.Status.ToString(),
        invoice.IsOverdue(today),
        invoice.SubTotal,
        invoice.SSTAmount,
        invoice.TotalAmount,
        invoice.AmountPaid,
        invoice.OutstandingBalance,
        invoice.Notes,
        invoice.VoidReason,
        invoice.Lines.Select(l => l.ToDto()).ToList());
}
