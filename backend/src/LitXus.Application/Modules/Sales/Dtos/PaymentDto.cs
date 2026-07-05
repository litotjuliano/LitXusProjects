using LitXus.Domain.Modules.Sales.Entities;

namespace LitXus.Application.Modules.Sales.Dtos;

public record PaymentDto(
    Guid Id,
    Guid InvoiceId,
    string? InvoiceNumber,
    DateOnly PaymentDate,
    decimal Amount,
    string Method,
    string? ReferenceNumber,
    string Status,
    Guid? VerifiedBy,
    DateTime? VerifiedAtUtc,
    Guid? BankAccountId,
    string? RejectReason);

public static class PaymentMappingExtensions
{
    public static PaymentDto ToDto(this Payment payment, string? invoiceNumber) => new(
        payment.Id, payment.InvoiceId, invoiceNumber, payment.PaymentDate, payment.Amount,
        payment.Method.ToString(), payment.ReferenceNumber, payment.Status.ToString(),
        payment.VerifiedBy, payment.VerifiedAtUtc, payment.BankAccountId, payment.RejectReason);
}
