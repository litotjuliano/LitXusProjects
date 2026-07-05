using LitXus.Application.Modules.Sales.Dtos;

namespace LitXus.Application.Modules.Sales.Commands.CreateInvoice;

/// <summary>
/// <see cref="CreditLimitWarning"/> is a soft, non-blocking warning — invoice creation always
/// succeeds regardless of credit limit, matching the "credit-limit warning on new invoice" wording
/// in docs/08_Sample_Data.md §8.4 rather than a hard rejection. A SalesUser may have good reason to
/// extend a trusted customer past their nominal limit, so this is surfaced for judgment, not enforced.
/// </summary>
public record CreateInvoiceResultDto(InvoiceDto Invoice, string? CreditLimitWarning);
