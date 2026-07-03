using ClosedXML.Excel;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Dtos;
using LitXus.Application.Modules.Company.Dtos;

namespace LitXus.Infrastructure.Services;

/// <summary>Excel (.xlsx) rendering for the 4 financial reports — see docs/03_API_Specification.md §3.5.</summary>
public class ClosedXmlReportExporter : IReportExcelExporter
{
    public byte[] ExportTrialBalance(TrialBalanceDto report, CompanyDto? company)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Trial Balance");
        var row = WriteTitle(ws, company, "Trial Balance", $"As of {report.AsOfDate:yyyy-MM-dd}");

        row = WriteHeaderRow(ws, row, "Code", "Account", "Type", "Debit", "Credit");
        foreach (var l in report.Lines)
        {
            ws.Cell(row, 1).Value = l.AccountCode;
            ws.Cell(row, 2).Value = l.AccountName;
            ws.Cell(row, 3).Value = l.AccountType;
            ws.Cell(row, 4).Value = l.Debit;
            ws.Cell(row, 5).Value = l.Credit;
            row++;
        }
        ws.Cell(row, 1).Value = "Total";
        ws.Cell(row, 4).Value = report.TotalDebit;
        ws.Cell(row, 5).Value = report.TotalCredit;
        ws.Row(row).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
        return ToBytes(workbook);
    }

    public byte[] ExportIncomeStatement(IncomeStatementDto report, CompanyDto? company)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Income Statement");
        var row = WriteTitle(ws, company, "Income Statement", $"{report.From:yyyy-MM-dd} to {report.To:yyyy-MM-dd}");

        ws.Cell(row, 1).Value = "Revenue";
        ws.Row(row).Style.Font.Bold = true;
        row++;
        foreach (var l in report.Revenue)
        {
            ws.Cell(row, 1).Value = $"{l.AccountCode} {l.AccountName}";
            ws.Cell(row, 2).Value = l.Amount;
            row++;
        }
        ws.Cell(row, 1).Value = "Total Revenue";
        ws.Cell(row, 2).Value = report.TotalRevenue;
        ws.Row(row).Style.Font.Bold = true;
        row += 2;

        ws.Cell(row, 1).Value = "Expenses";
        ws.Row(row).Style.Font.Bold = true;
        row++;
        foreach (var l in report.Expenses)
        {
            ws.Cell(row, 1).Value = $"{l.AccountCode} {l.AccountName}";
            ws.Cell(row, 2).Value = l.Amount;
            row++;
        }
        ws.Cell(row, 1).Value = "Total Expenses";
        ws.Cell(row, 2).Value = report.TotalExpenses;
        ws.Row(row).Style.Font.Bold = true;
        row += 2;

        ws.Cell(row, 1).Value = report.NetIncome >= 0 ? "Net Income" : "Net Loss";
        ws.Cell(row, 2).Value = report.NetIncome;
        ws.Row(row).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
        return ToBytes(workbook);
    }

    public byte[] ExportBalanceSheet(BalanceSheetDto report, CompanyDto? company)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Balance Sheet");
        var row = WriteTitle(ws, company, "Balance Sheet", $"As of {report.AsOfDate:yyyy-MM-dd}");

        row = WriteBalanceSheetSection(ws, row, "Assets", report.Assets, report.TotalAssets);
        row = WriteBalanceSheetSection(ws, row, "Liabilities", report.Liabilities, report.Liabilities.Sum(l => l.Balance));
        row = WriteBalanceSheetSection(ws, row, "Equity", report.Equity, report.Equity.Sum(l => l.Balance));

        ws.Cell(row, 1).Value = "Current Year Earnings";
        ws.Cell(row, 2).Value = report.CurrentYearEarnings;
        row++;
        ws.Cell(row, 1).Value = "Total Liabilities & Equity";
        ws.Cell(row, 2).Value = report.TotalLiabilitiesAndEquity;
        ws.Row(row).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
        return ToBytes(workbook);
    }

    public byte[] ExportGeneralLedger(GeneralLedgerDto report, CompanyDto? company)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("General Ledger");
        var row = WriteTitle(ws, company, $"General Ledger — {report.AccountCode} {report.AccountName}", $"{report.From:yyyy-MM-dd} to {report.To:yyyy-MM-dd}");

        row = WriteHeaderRow(ws, row, "Date", "Entry #", "Description", "Debit", "Credit", "Balance");
        foreach (var l in report.Lines)
        {
            ws.Cell(row, 1).Value = l.EntryDate.ToString("yyyy-MM-dd");
            ws.Cell(row, 2).Value = l.EntryNumber ?? "";
            ws.Cell(row, 3).Value = l.Description;
            ws.Cell(row, 4).Value = l.Debit;
            ws.Cell(row, 5).Value = l.Credit;
            ws.Cell(row, 6).Value = l.RunningBalance;
            row++;
        }
        ws.Cell(row, 5).Value = "Ending Balance";
        ws.Cell(row, 6).Value = report.EndingBalance;
        ws.Row(row).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
        return ToBytes(workbook);
    }

    private static int WriteBalanceSheetSection(IXLWorksheet ws, int row, string title, IReadOnlyList<BalanceSheetLineDto> lines, decimal total)
    {
        ws.Cell(row, 1).Value = title;
        ws.Row(row).Style.Font.Bold = true;
        row++;
        foreach (var l in lines)
        {
            ws.Cell(row, 1).Value = $"{l.AccountCode} {l.AccountName}";
            ws.Cell(row, 2).Value = l.Balance;
            row++;
        }
        ws.Cell(row, 1).Value = $"Total {title}";
        ws.Cell(row, 2).Value = total;
        ws.Row(row).Style.Font.Bold = true;
        return row + 2;
    }

    private static int WriteTitle(IXLWorksheet ws, CompanyDto? company, string title, string subtitle)
    {
        var row = 1;
        if (company is not null)
        {
            ws.Cell(row, 1).Value = company.Name;
            ws.Row(row).Style.Font.Bold = true;
            row++;
            ws.Cell(row, 1).Value = $"SSM: {company.SsmRegistrationNumber}  •  TIN: {company.Tin}";
            row++;
        }
        ws.Cell(row, 1).Value = title;
        ws.Row(row).Style.Font.Bold = true;
        ws.Row(row).Style.Font.FontSize = 14;
        row++;
        ws.Cell(row, 1).Value = subtitle;
        row += 2;
        return row;
    }

    private static int WriteHeaderRow(IXLWorksheet ws, int row, params string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(row, i + 1).Value = headers[i];
        }
        ws.Row(row).Style.Font.Bold = true;
        return row + 1;
    }

    private static byte[] ToBytes(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
