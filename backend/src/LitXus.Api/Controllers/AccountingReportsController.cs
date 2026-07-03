using LitXus.Api.Filters;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Queries.GetBalanceSheet;
using LitXus.Application.Modules.Accounting.Queries.GetGeneralLedger;
using LitXus.Application.Modules.Accounting.Queries.GetIncomeStatement;
using LitXus.Application.Modules.Accounting.Queries.GetTrialBalance;
using LitXus.Application.Modules.Company.Queries.GetCompanyProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LitXus.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/accounting/reports")]
[RequireModule(Module.Accounting)]
[RequirePermission("Accounting.Reports.Read")]
public class AccountingReportsController(
    IMediator mediator,
    IReportPdfExporter pdfExporter,
    IReportExcelExporter excelExporter) : ControllerBase
{
    private const string PdfContentType = "application/pdf";
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("trial-balance")]
    public async Task<IActionResult> TrialBalance([FromQuery] DateOnly? asOfDate)
    {
        var result = await mediator.Send(new GetTrialBalanceQuery(asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow)));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpGet("balance-sheet")]
    public async Task<IActionResult> BalanceSheet([FromQuery] DateOnly? asOfDate)
    {
        var result = await mediator.Send(new GetBalanceSheetQuery(asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow)));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpGet("income-statement")]
    public async Task<IActionResult> IncomeStatement([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await mediator.Send(new GetIncomeStatementQuery(
            from ?? new DateOnly(today.Year, 1, 1),
            to ?? today));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpGet("general-ledger")]
    public async Task<IActionResult> GeneralLedger([FromQuery] Guid accountId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await mediator.Send(new GetGeneralLedgerQuery(
            accountId,
            from ?? new DateOnly(today.Year, 1, 1),
            to ?? today));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpGet("trial-balance/pdf")]
    public async Task<IActionResult> TrialBalancePdf([FromQuery] DateOnly? asOfDate)
    {
        var report = await mediator.Send(new GetTrialBalanceQuery(asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow)));
        var company = await mediator.Send(new GetCompanyProfileQuery());
        return File(pdfExporter.ExportTrialBalance(report, company), PdfContentType, $"trial-balance-{report.AsOfDate:yyyy-MM-dd}.pdf");
    }

    [HttpGet("trial-balance/excel")]
    public async Task<IActionResult> TrialBalanceExcel([FromQuery] DateOnly? asOfDate)
    {
        var report = await mediator.Send(new GetTrialBalanceQuery(asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow)));
        var company = await mediator.Send(new GetCompanyProfileQuery());
        return File(excelExporter.ExportTrialBalance(report, company), ExcelContentType, $"trial-balance-{report.AsOfDate:yyyy-MM-dd}.xlsx");
    }

    [HttpGet("balance-sheet/pdf")]
    public async Task<IActionResult> BalanceSheetPdf([FromQuery] DateOnly? asOfDate)
    {
        var report = await mediator.Send(new GetBalanceSheetQuery(asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow)));
        var company = await mediator.Send(new GetCompanyProfileQuery());
        return File(pdfExporter.ExportBalanceSheet(report, company), PdfContentType, $"balance-sheet-{report.AsOfDate:yyyy-MM-dd}.pdf");
    }

    [HttpGet("balance-sheet/excel")]
    public async Task<IActionResult> BalanceSheetExcel([FromQuery] DateOnly? asOfDate)
    {
        var report = await mediator.Send(new GetBalanceSheetQuery(asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow)));
        var company = await mediator.Send(new GetCompanyProfileQuery());
        return File(excelExporter.ExportBalanceSheet(report, company), ExcelContentType, $"balance-sheet-{report.AsOfDate:yyyy-MM-dd}.xlsx");
    }

    [HttpGet("income-statement/pdf")]
    public async Task<IActionResult> IncomeStatementPdf([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var report = await mediator.Send(new GetIncomeStatementQuery(from ?? new DateOnly(today.Year, 1, 1), to ?? today));
        var company = await mediator.Send(new GetCompanyProfileQuery());
        return File(pdfExporter.ExportIncomeStatement(report, company), PdfContentType, $"income-statement-{report.From:yyyy-MM-dd}-to-{report.To:yyyy-MM-dd}.pdf");
    }

    [HttpGet("income-statement/excel")]
    public async Task<IActionResult> IncomeStatementExcel([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var report = await mediator.Send(new GetIncomeStatementQuery(from ?? new DateOnly(today.Year, 1, 1), to ?? today));
        var company = await mediator.Send(new GetCompanyProfileQuery());
        return File(excelExporter.ExportIncomeStatement(report, company), ExcelContentType, $"income-statement-{report.From:yyyy-MM-dd}-to-{report.To:yyyy-MM-dd}.xlsx");
    }

    [HttpGet("general-ledger/pdf")]
    public async Task<IActionResult> GeneralLedgerPdf([FromQuery] Guid accountId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var report = await mediator.Send(new GetGeneralLedgerQuery(accountId, from ?? new DateOnly(today.Year, 1, 1), to ?? today));
        var company = await mediator.Send(new GetCompanyProfileQuery());
        return File(pdfExporter.ExportGeneralLedger(report, company), PdfContentType, $"general-ledger-{report.AccountCode}-{report.From:yyyy-MM-dd}-to-{report.To:yyyy-MM-dd}.pdf");
    }

    [HttpGet("general-ledger/excel")]
    public async Task<IActionResult> GeneralLedgerExcel([FromQuery] Guid accountId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var report = await mediator.Send(new GetGeneralLedgerQuery(accountId, from ?? new DateOnly(today.Year, 1, 1), to ?? today));
        var company = await mediator.Send(new GetCompanyProfileQuery());
        return File(excelExporter.ExportGeneralLedger(report, company), ExcelContentType, $"general-ledger-{report.AccountCode}-{report.From:yyyy-MM-dd}-to-{report.To:yyyy-MM-dd}.xlsx");
    }
}
