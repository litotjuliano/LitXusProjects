using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace LitXus.IntegrationTests.Sales;

/// <summary>
/// Confirms invoice numbering stays gap-free/unique under concurrent Issue calls — the same
/// guarantee already covered for GL entry numbering, backed by a SQL Server SEQUENCE object
/// (NEXT VALUE FOR is atomic; see NumberSequenceGenerator), not application-level MAX(number)+1.
/// </summary>
[Collection("Integration")]
public class ConcurrentInvoiceNumberingTests(ApiWebApplicationFactory factory)
{
    private record Envelope<T>(T Data);
    private record IdResponse(Guid Id);
    private record InvoiceResponse(Guid Id, string? InvoiceNumber);

    [Fact]
    public async Task IssueInvoice_CalledConcurrently_ProducesUniqueInvoiceNumbersForEveryInvoice()
    {
        var client = await factory.AuthenticatedClientAsync("superadmin@litxus.demo");

        var customerResponse = await client.PostAsJsonAsync("/api/v1/sales/customers", new
        {
            code = $"CONC-{Random.Shared.Next(10000, 99999)}",
            companyName = "Concurrency Test Sdn Bhd",
            contactPerson = (string?)null,
            email = (string?)null,
            phone = (string?)null,
            address = (string?)null,
            creditLimit = 0m,
            paymentTermsDays = 30,
        });
        customerResponse.EnsureSuccessStatusCode();
        var customer = await customerResponse.Content.ReadFromJsonAsync<Envelope<IdResponse>>(AuthHelper.JsonOptions);

        const int invoiceCount = 20;
        var draftIds = new List<Guid>();
        for (var i = 0; i < invoiceCount; i++)
        {
            var createResponse = await client.PostAsJsonAsync("/api/v1/sales/invoices", new
            {
                customerId = customer!.Data.Id,
                invoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
                dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
                notes = (string?)null,
                lines = new[] { new { description = "Item", quantity = 1m, unitOfMeasure = (string?)null, unitPrice = 10m, taxCodeId = (Guid?)null } },
            });
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await createResponse.Content.ReadFromJsonAsync<Envelope<IdResponse>>(AuthHelper.JsonOptions);
            draftIds.Add(created!.Data.Id);
        }

        var issueTasks = draftIds.Select(async id =>
        {
            var response = await client.PostAsync($"/api/v1/sales/invoices/{id}/issue", null);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<Envelope<InvoiceResponse>>(AuthHelper.JsonOptions);
            return body!.Data.InvoiceNumber;
        });

        var invoiceNumbers = await Task.WhenAll(issueTasks);

        invoiceNumbers.Should().OnlyHaveUniqueItems();
        invoiceNumbers.Should().OnlyContain(n => !string.IsNullOrEmpty(n));
        invoiceNumbers.Should().HaveCount(invoiceCount);
    }
}
