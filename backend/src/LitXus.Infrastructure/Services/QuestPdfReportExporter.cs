using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using LitXus.Application.Modules.Company.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LitXus.Infrastructure.Services;

/// <summary>PDF rendering for the 4 financial reports — see docs/03_API_Specification.md §3.5.</summary>
public class QuestPdfReportExporter : IReportPdfExporter
{
    public byte[] ExportTrialBalance(TrialBalanceDto report, CompanyDto? company) =>
        BuildDocument("Trial Balance", $"As of {report.AsOfDate:yyyy-MM-dd}", company, content =>
        {
            content.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2);
                    c.RelativeColumn(4);
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                });
                HeaderRow(table, "Code", "Account", "Type", "Debit", "Credit");
                foreach (var l in report.Lines)
                {
                    Cell(table, l.AccountCode);
                    Cell(table, l.AccountName);
                    Cell(table, l.AccountType);
                    CellRight(table, l.Debit.ToString("N2"));
                    CellRight(table, l.Credit.ToString("N2"));
                }
                Cell(table, "Total", bold: true);
                Cell(table, "");
                Cell(table, "");
                CellRight(table, report.TotalDebit.ToString("N2"), bold: true);
                CellRight(table, report.TotalCredit.ToString("N2"), bold: true);
            });
        });

    public byte[] ExportIncomeStatement(IncomeStatementDto report, CompanyDto? company) =>
        BuildDocument("Income Statement", $"{report.From:yyyy-MM-dd} to {report.To:yyyy-MM-dd}", company, content =>
        {
            content.Item().Column(col =>
            {
                col.Item().Text("Revenue").Bold();
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(6); c.RelativeColumn(2); });
                    foreach (var l in report.Revenue)
                    {
                        Cell(table, $"{l.AccountCode} {l.AccountName}");
                        CellRight(table, l.Amount.ToString("N2"));
                    }
                    Cell(table, "Total Revenue", bold: true);
                    CellRight(table, report.TotalRevenue.ToString("N2"), bold: true);
                });

                col.Item().PaddingTop(10).Text("Expenses").Bold();
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(6); c.RelativeColumn(2); });
                    foreach (var l in report.Expenses)
                    {
                        Cell(table, $"{l.AccountCode} {l.AccountName}");
                        CellRight(table, l.Amount.ToString("N2"));
                    }
                    Cell(table, "Total Expenses", bold: true);
                    CellRight(table, report.TotalExpenses.ToString("N2"), bold: true);
                });

                col.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text(report.NetIncome >= 0 ? "Net Income" : "Net Loss").Bold();
                    row.RelativeItem().AlignRight().Text(report.NetIncome.ToString("N2")).Bold();
                });
            });
        });

    public byte[] ExportBalanceSheet(BalanceSheetDto report, CompanyDto? company) =>
        BuildDocument("Balance Sheet", $"As of {report.AsOfDate:yyyy-MM-dd}", company, content =>
        {
            content.Item().Column(col =>
            {
                BalanceSheetSection(col, "Assets", report.Assets, report.TotalAssets);
                BalanceSheetSection(col, "Liabilities", report.Liabilities, report.Liabilities.Sum(l => l.Balance));
                BalanceSheetSection(col, "Equity", report.Equity, report.Equity.Sum(l => l.Balance));

                col.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text("Current Year Earnings");
                    row.RelativeItem().AlignRight().Text(report.CurrentYearEarnings.ToString("N2"));
                });
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text("Total Liabilities & Equity").Bold();
                    row.RelativeItem().AlignRight().Text(report.TotalLiabilitiesAndEquity.ToString("N2")).Bold();
                });
            });
        });

    public byte[] ExportGeneralLedger(GeneralLedgerDto report, CompanyDto? company) =>
        BuildDocument($"General Ledger — {report.AccountCode} {report.AccountName}", $"{report.From:yyyy-MM-dd} to {report.To:yyyy-MM-dd}", company, content =>
        {
            content.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                    c.RelativeColumn(4);
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                });
                HeaderRow(table, "Date", "Entry #", "Description", "Debit", "Credit", "Balance");
                foreach (var l in report.Lines)
                {
                    Cell(table, l.EntryDate.ToString("yyyy-MM-dd"));
                    Cell(table, l.EntryNumber ?? "—");
                    Cell(table, l.Description);
                    CellRight(table, l.Debit.ToString("N2"));
                    CellRight(table, l.Credit.ToString("N2"));
                    CellRight(table, l.RunningBalance.ToString("N2"));
                }
                Cell(table, "");
                Cell(table, "");
                Cell(table, "");
                Cell(table, "");
                Cell(table, "Ending Balance", bold: true);
                CellRight(table, report.EndingBalance.ToString("N2"), bold: true);
            });
        });

    private static void BalanceSheetSection(ColumnDescriptor col, string title, IReadOnlyList<BalanceSheetLineDto> lines, decimal total)
    {
        col.Item().PaddingTop(10).Text(title).Bold();
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c => { c.RelativeColumn(6); c.RelativeColumn(2); });
            foreach (var l in lines)
            {
                Cell(table, $"{l.AccountCode} {l.AccountName}");
                CellRight(table, l.Balance.ToString("N2"));
            }
            Cell(table, $"Total {title}", bold: true);
            CellRight(table, total.ToString("N2"), bold: true);
        });
    }

    private static byte[] BuildDocument(string title, string subtitle, CompanyDto? company, Action<ColumnDescriptor> content) =>
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    if (company is not null)
                    {
                        col.Item().AlignCenter().Text(company.Name).FontSize(13).Bold();
                        col.Item().AlignCenter().Text($"{company.AddressLine1}, {company.City}, {company.State}").FontSize(8);
                        col.Item().AlignCenter().Text($"SSM: {company.SsmRegistrationNumber}  •  TIN: {company.Tin}").FontSize(8);
                    }
                    col.Item().PaddingTop(10).AlignCenter().Text(title).FontSize(14).Bold();
                    col.Item().AlignCenter().Text(subtitle).FontSize(9);
                });

                page.Content().PaddingTop(15).Column(content);

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();

    private static void HeaderRow(TableDescriptor table, params string[] headers)
    {
        foreach (var h in headers)
        {
            table.Cell().BorderBottom(1).PaddingBottom(4).Text(h).Bold();
        }
    }

    private static void Cell(TableDescriptor table, string text, bool bold = false)
    {
        var t = table.Cell().PaddingVertical(2).Text(text);
        if (bold) t.Bold();
    }

    private static void CellRight(TableDescriptor table, string text, bool bold = false)
    {
        var t = table.Cell().PaddingVertical(2).AlignRight().Text(text);
        if (bold) t.Bold();
    }
}
