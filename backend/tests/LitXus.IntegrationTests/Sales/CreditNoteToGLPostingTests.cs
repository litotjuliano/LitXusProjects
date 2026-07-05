using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace LitXus.IntegrationTests.Sales;

/// <summary>
/// Proves CreditNote.Create() -> CreditNoteAppliedEvent -> PostCreditNoteToGLHandler actually posts
/// a real, balanced GL entry (Dr Sales Revenue, Cr Accounts Receivable) end-to-end.
/// </summary>
[Collection("Integration")]
public class CreditNoteToGLPostingTests(ApiWebApplicationFactory factory)
{
    private record Envelope<T>(T Data);
    private record IdResponse(Guid Id);
    private record InvoiceResponse(Guid Id, string? InvoiceNumber, string Status);
    private record CreditNoteResponse(Guid Id, string? CreditNoteNumber, string Status);
    private record GLEntryLineResponse(Guid AccountId, decimal DebitAmount, decimal CreditAmount);
    private record GLEntryResponse(string Description, string Status, IReadOnlyList<GLEntryLineResponse> Lines);

    private static async Task<Guid> CreateAccountAsync(HttpClient client, string type)
    {
        var code = $"9{Random.Shared.Next(10000, 99999)}";
        var response = await client.PostAsJsonAsync("/api/v1/accounting/accounts", new
        {
            code,
            name = $"Credit Note Test {type} Account",
            type,
            parentAccountId = (Guid?)null,
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Envelope<IdResponse>>(AuthHelper.JsonOptions);
        return body!.Data.Id;
    }

    [Fact]
    public async Task CreateCreditNote_PostsBalancedGLEntryReversingRevenue()
    {
        var client = await factory.AuthenticatedClientAsync("superadmin@litxus.demo");

        var receivableAccountId = await CreateAccountAsync(client, "Asset");
        var revenueAccountId = await CreateAccountAsync(client, "Revenue");
        var taxPayableAccountId = await CreateAccountAsync(client, "Liability");
        var cashAccountId = await CreateAccountAsync(client, "Asset");

        (await client.PutAsJsonAsync("/api/v1/sales/settings", new
        {
            receivableAccountId,
            revenueAccountId,
            taxPayableAccountId,
            cashAccountId,
        })).EnsureSuccessStatusCode();

        var customerResponse = await client.PostAsJsonAsync("/api/v1/sales/customers", new
        {
            code = $"CNGL-{Random.Shared.Next(10000, 99999)}",
            companyName = "Credit Note GL Test Sdn Bhd",
            contactPerson = (string?)null,
            email = (string?)null,
            phone = (string?)null,
            address = (string?)null,
            creditLimit = 0m,
            paymentTermsDays = 30,
        });
        customerResponse.EnsureSuccessStatusCode();
        var customer = await customerResponse.Content.ReadFromJsonAsync<Envelope<IdResponse>>(AuthHelper.JsonOptions);

        var invoiceResponse = await client.PostAsJsonAsync("/api/v1/sales/invoices", new
        {
            customerId = customer!.Data.Id,
            invoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
            notes = (string?)null,
            lines = new[] { new { description = "Widget", quantity = 1m, unitOfMeasure = (string?)null, unitPrice = 1000m, taxCodeId = (Guid?)null } },
        });
        invoiceResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var invoice = await invoiceResponse.Content.ReadFromJsonAsync<Envelope<InvoiceResponse>>(AuthHelper.JsonOptions);
        var invoiceId = invoice!.Data.Id;

        var issueResponse = await client.PostAsync($"/api/v1/sales/invoices/{invoiceId}/issue", null);
        issueResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var issued = await issueResponse.Content.ReadFromJsonAsync<Envelope<InvoiceResponse>>(AuthHelper.JsonOptions);
        var invoiceNumber = issued!.Data.InvoiceNumber;

        var creditNoteResponse = await client.PostAsJsonAsync("/api/v1/sales/credit-notes", new
        {
            invoiceId,
            reason = "3 units damaged in transit",
            amount = 200m,
        });
        creditNoteResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var creditNote = await creditNoteResponse.Content.ReadFromJsonAsync<Envelope<CreditNoteResponse>>(AuthHelper.JsonOptions);
        creditNote!.Data.Status.Should().Be("Applied");
        var creditNoteNumber = creditNote.Data.CreditNoteNumber;
        creditNoteNumber.Should().NotBeNullOrEmpty();

        var glResponse = await client.GetAsync("/api/v1/accounting/gl-entries");
        glResponse.EnsureSuccessStatusCode();
        var glEntries = await glResponse.Content.ReadFromJsonAsync<Envelope<List<GLEntryResponse>>>(AuthHelper.JsonOptions);

        var postedEntry = glEntries!.Data.Should().ContainSingle(e => e.Description == $"Credit note {creditNoteNumber}").Subject;
        postedEntry.Status.Should().Be("Posted");

        var debitLine = postedEntry.Lines.Should().ContainSingle(l => l.AccountId == revenueAccountId).Subject;
        debitLine.DebitAmount.Should().Be(200m);
        debitLine.CreditAmount.Should().Be(0m);

        var creditLine = postedEntry.Lines.Should().ContainSingle(l => l.AccountId == receivableAccountId).Subject;
        creditLine.CreditAmount.Should().Be(200m);
        creditLine.DebitAmount.Should().Be(0m);
    }
}
