using FluentAssertions;
using LitXus.Domain.Modules.Sales.Entities;
using LitXus.Domain.Modules.Sales.Enums;
using LitXus.Domain.Modules.Sales.Events;
using LitXus.Domain.Modules.Sales.Exceptions;

namespace LitXus.UnitTests.Domain;

public class PaymentTests
{
    private static Payment PendingPayment(decimal amount = 100m) =>
        Payment.Create(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), amount, PaymentMethod.BankTransfer, "REF-001", null);

    [Fact]
    public void Create_StartsPendingAndDoesNotRaiseAnyEvent()
    {
        var payment = PendingPayment();

        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Verify_WhenPending_TransitionsToVerifiedAndRaisesPaymentVerifiedEvent()
    {
        var payment = PendingPayment();
        var verifiedBy = Guid.NewGuid();
        var verifiedAt = DateTime.UtcNow;

        payment.Verify(verifiedBy, verifiedAt);

        payment.Status.Should().Be(PaymentStatus.Verified);
        payment.VerifiedBy.Should().Be(verifiedBy);
        payment.VerifiedAtUtc.Should().Be(verifiedAt);
        payment.DomainEvents.Should().ContainSingle(e => e is PaymentVerifiedEvent);
    }

    [Fact]
    public void Verify_WhenAlreadyVerified_ThrowsPaymentNotPendingException()
    {
        var payment = PendingPayment();
        payment.Verify(Guid.NewGuid(), DateTime.UtcNow);

        var act = () => payment.Verify(Guid.NewGuid(), DateTime.UtcNow);

        act.Should().Throw<PaymentNotPendingException>();
    }

    [Fact]
    public void Reject_WhenPendingWithReason_TransitionsToRejected()
    {
        var payment = PendingPayment();

        payment.Reject("Bank reference could not be verified");

        payment.Status.Should().Be(PaymentStatus.Rejected);
        payment.RejectReason.Should().Be("Bank reference could not be verified");
    }

    [Fact]
    public void Reject_WithEmptyReason_ThrowsRejectRequiresReasonException()
    {
        var payment = PendingPayment();

        var act = () => payment.Reject("   ");

        act.Should().Throw<RejectRequiresReasonException>();
        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public void Reject_WhenAlreadyVerified_ThrowsPaymentNotPendingException()
    {
        var payment = PendingPayment();
        payment.Verify(Guid.NewGuid(), DateTime.UtcNow);

        var act = () => payment.Reject("Too late");

        act.Should().Throw<PaymentNotPendingException>();
    }
}
