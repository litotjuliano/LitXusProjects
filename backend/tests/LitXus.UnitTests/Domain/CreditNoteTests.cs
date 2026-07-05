using FluentAssertions;
using LitXus.Domain.Modules.Sales.Entities;
using LitXus.Domain.Modules.Sales.Enums;

namespace LitXus.UnitTests.Domain;

public class CreditNoteTests
{
    [Fact]
    public void Create_AssignsNumberAndSetsStatusAppliedDirectly()
    {
        var invoiceId = Guid.NewGuid();

        var creditNote = CreditNote.Create("CN-2026-000001", invoiceId, "Damaged goods returned", 150m);

        creditNote.CreditNoteNumber.Should().Be("CN-2026-000001");
        creditNote.InvoiceId.Should().Be(invoiceId);
        creditNote.Reason.Should().Be("Damaged goods returned");
        creditNote.Amount.Should().Be(150m);
        creditNote.Status.Should().Be(CreditNoteStatus.Applied);
    }
}
