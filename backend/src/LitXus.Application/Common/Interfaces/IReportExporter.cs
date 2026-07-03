using LitXus.Application.Modules.Accounting.Dtos;
using LitXus.Application.Modules.Company.Dtos;

namespace LitXus.Application.Common.Interfaces;

/// <summary>Renders an already-fetched report DTO to a downloadable file — see docs/03_API_Specification.md §3.5.</summary>
public interface IReportPdfExporter
{
    byte[] ExportTrialBalance(TrialBalanceDto report, CompanyDto? company);
    byte[] ExportIncomeStatement(IncomeStatementDto report, CompanyDto? company);
    byte[] ExportBalanceSheet(BalanceSheetDto report, CompanyDto? company);
    byte[] ExportGeneralLedger(GeneralLedgerDto report, CompanyDto? company);
}

public interface IReportExcelExporter
{
    byte[] ExportTrialBalance(TrialBalanceDto report, CompanyDto? company);
    byte[] ExportIncomeStatement(IncomeStatementDto report, CompanyDto? company);
    byte[] ExportBalanceSheet(BalanceSheetDto report, CompanyDto? company);
    byte[] ExportGeneralLedger(GeneralLedgerDto report, CompanyDto? company);
}
