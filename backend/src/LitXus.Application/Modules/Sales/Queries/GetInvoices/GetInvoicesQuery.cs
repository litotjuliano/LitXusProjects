using LitXus.Application.Modules.Sales.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Sales.Queries.GetInvoices;

public record GetInvoicesQuery(string? Status = null, Guid? CustomerId = null, DateOnly? DateFrom = null, DateOnly? DateTo = null)
    : IRequest<IReadOnlyList<InvoiceDto>>;
