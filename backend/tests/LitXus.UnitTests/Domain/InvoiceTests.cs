using FluentAssertions;
using LitXus.Domain.Modules.Accounting.Entities;
using LitXus.Domain.Modules.Accounting.Enums;
using LitXus.Domain.Modules.Sales.Entities;
using LitXus.Domain.Modules.Sales.Enums;
using LitXus.Domain.Modules.Sales.Events;
using LitXus.Domain.Modules.Sales.Exceptions;

namespace LitXus.UnitTests.Domain;

public class InvoiceTests
{
    private static InvoiceLine Line(string description, decimal quantity, decimal unitPrice, TaxCode? taxCode = null) =>
        InvoiceLine.Create(description, quantity, "pcs", unitPrice, taxCode);

    private static Invoice DraftInvoice(decimal unitPrice = 100m, TaxCode? taxCode = null) =>
        Invoice.CreateDraft(
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
            null,
            [Line("Widget", 2, unitPrice, taxCode)]);

    [Fact]
    public void CreateDraft_ComputesSubTotalAndTotalFromLines()
    {
        var invoice = DraftInvoice(unitPrice: 100m);

        invoice.Status.Should().Be(InvoiceStatus.Draft);
        invoice.SubTotal.Should().Be(200m);
        invoice.SSTAmount.Should().Be(0m);
        invoice.TotalAmount.Should().Be(200m);
        invoice.OutstandingBalance.Should().Be(200m);
    }

    [Fact]
    public void CreateDraft_WithTaxedLine_IncludesSSTInTotal()
    {
        var sst = TaxCode.Create("SST-6", "Sales & Service Tax 6%", 6.00m, TaxType.Sst);
        var invoice = DraftInvoice(unitPrice: 500m, taxCode: sst);

        invoice.SubTotal.Should().Be(1000m);
        invoice.SSTAmount.Should().Be(60.00m);
        invoice.TotalAmount.Should().Be(1060.00m);
    }

    [Fact]
    public void Issue_AssignsNumberTransitionsToIssuedAndRaisesInvoiceIssuedEvent()
    {
        var invoice = DraftInvoice();

        invoice.Issue("INV-2026-000001");

        invoice.InvoiceNumber.Should().Be("INV-2026-000001");
        invoice.Status.Should().Be(InvoiceStatus.Issued);
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceIssuedEvent);
    }

    [Fact]
    public void Issue_WhenAlreadyIssued_ThrowsInvoiceNotDraftException()
    {
        var invoice = DraftInvoice();
        invoice.Issue("INV-2026-000001");

        var act = () => invoice.Issue("INV-2026-000002");

        act.Should().Throw<InvoiceNotDraftException>();
    }

    [Fact]
    public void UpdateLines_WhenNotDraft_ThrowsInvoiceNotDraftException()
    {
        var invoice = DraftInvoice();
        invoice.Issue("INV-2026-000001");

        var act = () => invoice.UpdateLines(invoice.InvoiceDate, invoice.DueDate, null, [Line("Widget", 1, 50m)]);

        act.Should().Throw<InvoiceNotDraftException>();
    }

    [Fact]
    public void ApplyPayment_PartialAmount_TransitionsToPartiallyPaid()
    {
        var invoice = DraftInvoice(unitPrice: 100m); // total 200
        invoice.Issue("INV-2026-000001");

        invoice.ApplyPayment(50m);

        invoice.AmountPaid.Should().Be(50m);
        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
        invoice.OutstandingBalance.Should().Be(150m);
    }

    [Fact]
    public void ApplyPayment_FullAmount_TransitionsToPaid()
    {
        var invoice = DraftInvoice(unitPrice: 100m); // total 200
        invoice.Issue("INV-2026-000001");

        invoice.ApplyPayment(200m);

        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.OutstandingBalance.Should().Be(0m);
    }

    [Fact]
    public void ApplyPayment_ExceedingOutstandingBalance_ThrowsPaymentExceedsOutstandingBalanceException()
    {
        var invoice = DraftInvoice(unitPrice: 100m); // total 200
        invoice.Issue("INV-2026-000001");

        var act = () => invoice.ApplyPayment(250m);

        act.Should().Throw<PaymentExceedsOutstandingBalanceException>();
        invoice.AmountPaid.Should().Be(0m);
    }

    [Fact]
    public void Void_WithNoVerifiedPayment_TransitionsToVoidAndRecordsReason()
    {
        var invoice = DraftInvoice();
        invoice.Issue("INV-2026-000001");

        invoice.Void("Customer cancelled the order", hasVerifiedPayment: false);

        invoice.Status.Should().Be(InvoiceStatus.Void);
        invoice.VoidReason.Should().Be("Customer cancelled the order");
    }

    [Fact]
    public void Void_WithVerifiedPayment_ThrowsInvoiceHasVerifiedPaymentException()
    {
        var invoice = DraftInvoice();
        invoice.Issue("INV-2026-000001");

        var act = () => invoice.Void("Trying to void anyway", hasVerifiedPayment: true);

        act.Should().Throw<InvoiceHasVerifiedPaymentException>();
        invoice.Status.Should().Be(InvoiceStatus.Issued);
    }

    [Fact]
    public void Void_WhenStillDraft_ThrowsInvoiceNotVoidableException()
    {
        var invoice = DraftInvoice();

        var act = () => invoice.Void("reason", hasVerifiedPayment: false);

        act.Should().Throw<InvoiceNotVoidableException>();
    }

    [Fact]
    public void Void_WithEmptyReason_ThrowsInvoiceVoidRequiresReasonException()
    {
        var invoice = DraftInvoice();
        invoice.Issue("INV-2026-000001");

        var act = () => invoice.Void("   ", hasVerifiedPayment: false);

        act.Should().Throw<InvoiceVoidRequiresReasonException>();
    }

    [Fact]
    public void IsOverdue_WhenIssuedAndPastDueDate_ReturnsTrue()
    {
        var invoice = Invoice.CreateDraft(
            Guid.NewGuid(),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            null,
            [Line("Widget", 1, 100m)]);
        invoice.Issue("INV-2026-000001");

        invoice.IsOverdue(new DateOnly(2026, 2, 15)).Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_WhenPaid_ReturnsFalseEvenPastDueDate()
    {
        var invoice = Invoice.CreateDraft(
            Guid.NewGuid(),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            null,
            [Line("Widget", 1, 100m)]);
        invoice.Issue("INV-2026-000001");
        invoice.ApplyPayment(100m);

        invoice.IsOverdue(new DateOnly(2026, 2, 15)).Should().BeFalse();
    }
}
