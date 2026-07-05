using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Company.Dtos;
using LitXus.Application.Modules.Sales.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace LitXus.Infrastructure.Services;

/// <summary>PDF rendering for a single Sales invoice — see docs/phase-2-sales/API_Specification.md.
/// Reuses QuestPdfReportExporter's page-layout/cell helpers rather than duplicating them.</summary>
public class QuestPdfInvoiceExporter : IInvoicePdfExporter
{
    public byte[] ExportInvoice(InvoiceDto invoice, CompanyDto? company) =>
        QuestPdfReportExporter.BuildDocument(
            $"Invoice {invoice.InvoiceNumber ?? "(Draft)"}",
            $"{invoice.CustomerCode} — {invoice.CustomerName}",
            company,
            content =>
            {
                content.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Invoice Date: {invoice.InvoiceDate:yyyy-MM-dd}");
                    row.RelativeItem().AlignRight().Text($"Due Date: {invoice.DueDate:yyyy-MM-dd}");
                });
                content.Item().PaddingTop(4).Text($"Status: {invoice.Status}{(invoice.IsOverdue ? " (Overdue)" : "")}");

                content.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(4);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                    });
                    QuestPdfReportExporter.HeaderRow(table, "Description", "Qty", "UOM", "Unit Price", "Line Total");
                    foreach (var l in invoice.Lines)
                    {
                        QuestPdfReportExporter.Cell(table, l.Description);
                        QuestPdfReportExporter.CellRight(table, l.Quantity.ToString("N2"));
                        QuestPdfReportExporter.Cell(table, l.UnitOfMeasure ?? "—");
                        QuestPdfReportExporter.CellRight(table, l.UnitPrice.ToString("N2"));
                        QuestPdfReportExporter.CellRight(table, l.LineTotal.ToString("N2"));
                    }
                });

                content.Item().PaddingTop(10).AlignRight().Column(col =>
                {
                    col.Item().Text($"Subtotal: {invoice.SubTotal:N2}");
                    col.Item().Text($"SST: {invoice.SSTAmount:N2}");
                    col.Item().Text($"Total: {invoice.TotalAmount:N2}").Bold();
                    col.Item().Text($"Amount Paid: {invoice.AmountPaid:N2}");
                    col.Item().Text($"Outstanding: {invoice.OutstandingBalance:N2}").Bold();
                });

                if (!string.IsNullOrWhiteSpace(invoice.Notes))
                {
                    content.Item().PaddingTop(10).Text($"Notes: {invoice.Notes}");
                }
            });
}
