using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace LitXus.IntegrationTests.Sales;

/// <summary>
/// Proves the domain-event dispatch actually fires end-to-end: Invoice.Issue() -> InvoiceIssuedEvent
/// -> DomainEventDispatchInterceptor (after SaveChangesAsync commits) -> PostInvoiceToGLHandler ->
/// a real Posted GLEntry, all through the real HTTP/DB stack, not a unit-level fake.
/// </summary>
[Collection("Integration")]
public class InvoiceToGLPostingTests(ApiWebApplicationFactory factory)
{
    private record Envelope<T>(T Data);
    private record IdResponse(Guid Id);
    private record InvoiceResponse(Guid Id, string? InvoiceNumber, string Status, decimal TotalAmount, decimal OutstandingBalance);
    private record GLEntryLineResponse(Guid AccountId, string AccountCode, decimal DebitAmount, decimal CreditAmount);
    private record GLEntryResponse(Guid Id, string? EntryNumber, string Description, string Status, IReadOnlyList<GLEntryLineResponse> Lines);

    private static async Task<Guid> CreateAccountAsync(HttpClient client, string type)
    {
        var code = $"9{Random.Shared.Next(10000, 99999)}";
        var response = await client.PostAsJsonAsync("/api/v1/accounting/accounts", new
        {
            code,
            name = $"Sales Test {type} Account",
            type,
            parentAccountId = (Guid?)null,
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Envelope<IdResponse>>(AuthHelper.JsonOptions);
        return body!.Data.Id;
    }

    [Fact]
    public async Task IssueInvoice_PostsBalancedGLEntryTaggedSalesAutoPost()
    {
        var client = await factory.AuthenticatedClientAsync("superadmin@litxus.demo");

        var receivableAccountId = await CreateAccountAsync(client, "Asset");
        var revenueAccountId = await CreateAccountAsync(client, "Revenue");
        var taxPayableAccountId = await CreateAccountAsync(client, "Liability");
        var cashAccountId = await CreateAccountAsync(client, "Asset");

        var settingsResponse = await client.PutAsJsonAsync("/api/v1/sales/settings", new
        {
            receivableAccountId,
            revenueAccountId,
            taxPayableAccountId,
            cashAccountId,
        });
        settingsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var customerCode = $"GLTEST-{Random.Shared.Next(10000, 99999)}";
        var customerResponse = await client.PostAsJsonAsync("/api/v1/sales/customers", new
        {
            code = customerCode,
            companyName = "GL Posting Test Sdn Bhd",
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
            lines = new[]
            {
                new { description = "Integration test widget", quantity = 2m, unitOfMeasure = "pcs", unitPrice = 500m, taxCodeId = (Guid?)null },
            },
        });
        invoiceResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var invoice = await invoiceResponse.Content.ReadFromJsonAsync<Envelope<InvoiceResponse>>(AuthHelper.JsonOptions);
        invoice!.Data.Status.Should().Be("Draft");
        var invoiceId = invoice.Data.Id;

        var issueResponse = await client.PostAsync($"/api/v1/sales/invoices/{invoiceId}/issue", null);
        issueResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var issued = await issueResponse.Content.ReadFromJsonAsync<Envelope<InvoiceResponse>>(AuthHelper.JsonOptions);
        issued!.Data.Status.Should().Be("Issued");
        issued.Data.TotalAmount.Should().Be(1000m);
        var invoiceNumber = issued.Data.InvoiceNumber;
        invoiceNumber.Should().NotBeNullOrEmpty();

        // The domain event has to be dispatched and the GL entry committed to the same database
        // this HTTP client reads from — no manual wait needed since DomainEventDispatchInterceptor
        // runs synchronously inside SaveChangesAsync before the Issue request's response is returned.
        var glResponse = await client.GetAsync("/api/v1/accounting/gl-entries");
        glResponse.EnsureSuccessStatusCode();
        var glEntries = await glResponse.Content.ReadFromJsonAsync<Envelope<List<GLEntryResponse>>>(AuthHelper.JsonOptions);

        var postedEntry = glEntries!.Data.Should().ContainSingle(e => e.Description == $"Sales invoice {invoiceNumber}").Subject;
        postedEntry.Status.Should().Be("Posted");
        postedEntry.EntryNumber.Should().NotBeNullOrEmpty();

        var debitLine = postedEntry.Lines.Should().ContainSingle(l => l.AccountId == receivableAccountId).Subject;
        debitLine.DebitAmount.Should().Be(1000m);
        debitLine.CreditAmount.Should().Be(0m);

        var creditLine = postedEntry.Lines.Should().ContainSingle(l => l.AccountId == revenueAccountId).Subject;
        creditLine.CreditAmount.Should().Be(1000m);
        creditLine.DebitAmount.Should().Be(0m);
    }
}
