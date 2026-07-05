namespace LitXus.Application.Modules.Sales.Dtos;

public record SalesSummaryLineDto(string GroupKey, decimal SubTotal, decimal SSTAmount, decimal TotalAmount, int InvoiceCount);

public record SalesSummaryDto(
    DateOnly From,
    DateOnly To,
    string GroupBy,
    IReadOnlyList<SalesSummaryLineDto> Lines,
    decimal GrandTotal);

public record ArAgingLineDto(
    Guid CustomerId,
    string CustomerCode,
    string CustomerName,
    decimal Current,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Over90Days,
    decimal Total);

public record ArAgingDto(DateOnly AsOfDate, IReadOnlyList<ArAgingLineDto> Lines, decimal GrandTotal);
