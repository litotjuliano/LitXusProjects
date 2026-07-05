using LitXus.Domain.Common;
using LitXus.Domain.Modules.Sales.Enums;
using LitXus.Domain.Modules.Sales.Events;
using LitXus.Domain.Modules.Sales.Exceptions;

namespace LitXus.Domain.Modules.Sales.Entities;

public class Payment : BaseEntity, IAuditable
{
    public Guid InvoiceId { get; private set; }
    public DateOnly PaymentDate { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public Guid? VerifiedBy { get; private set; }
    public DateTime? VerifiedAtUtc { get; private set; }
    public Guid? BankAccountId { get; private set; }
    public string? RejectReason { get; private set; }

    private Payment() { }

    public static Payment Create(Guid invoiceId, DateOnly paymentDate, decimal amount, PaymentMethod method, string? referenceNumber, Guid? bankAccountId)
    {
        return new Payment
        {
            InvoiceId = invoiceId,
            PaymentDate = paymentDate,
            Amount = amount,
            Method = method,
            ReferenceNumber = referenceNumber,
            BankAccountId = bankAccountId,
        };
    }

    /// <summary>Raises PaymentVerifiedEvent — this, not recording, is what applies the amount to
    /// the invoice and auto-posts the GL entry (docs/01_Architecture.md §1.4); a merely-recorded
    /// Pending payment hasn't been confirmed to have actually happened yet.</summary>
    public void Verify(Guid verifiedBy, DateTime verifiedAtUtc)
    {
        EnsureIsPending();
        Status = PaymentStatus.Verified;
        VerifiedBy = verifiedBy;
        VerifiedAtUtc = verifiedAtUtc;
        AddDomainEvent(new PaymentVerifiedEvent(Id));
    }

    public void Reject(string reason)
    {
        EnsureIsPending();

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new RejectRequiresReasonException();
        }

        Status = PaymentStatus.Rejected;
        RejectReason = reason;
    }

    private void EnsureIsPending()
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new PaymentNotPendingException();
        }
    }
}
