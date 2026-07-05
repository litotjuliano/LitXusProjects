using LitXus.Application.Modules.Company.Dtos;
using LitXus.Application.Modules.Sales.Dtos;

namespace LitXus.Application.Common.Interfaces;

/// <summary>Renders an already-fetched InvoiceDto to a downloadable PDF — see
/// docs/phase-2-sales/API_Specification.md.</summary>
public interface IInvoicePdfExporter
{
    byte[] ExportInvoice(InvoiceDto invoice, CompanyDto? company);
}
