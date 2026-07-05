using System.Net.Http.Json;
using FluentAssertions;

namespace LitXus.IntegrationTests.Sales;

/// <summary>
/// CreditLimit is a soft, non-blocking check (docs/phase-2-sales/Business_Rules.md) — invoice
/// creation always succeeds; only the response's meta.creditLimitWarning differs.
/// </summary>
[Collection("Integration")]
public class CreditLimitWarningTests(ApiWebApplicationFactory factory)
{
    private record Envelope<T>(T Data, MetaEnvelope? Meta);
    private record MetaEnvelope(string? CreditLimitWarning);
    private record IdResponse(Guid Id);

    private static async Task<Guid> CreateCustomerAsync(HttpClient client, decimal creditLimit)
    {
        var code = $"CLTEST-{Random.Shared.Next(10000, 99999)}";
        var response = await client.PostAsJsonAsync("/api/v1/sales/customers", new
        {
            code,
            companyName = "Credit Limit Test Sdn Bhd",
            contactPerson = (string?)null,
            email = (string?)null,
            phone = (string?)null,
            address = (string?)null,
            creditLimit,
            paymentTermsDays = 30,
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Envelope<IdResponse>>(AuthHelper.JsonOptions);
        return body!.Data.Id;
    }

    private static async Task<Guid> CreateAndIssueInvoiceAsync(HttpClient client, Guid customerId, decimal amount)
    {
        var createResponse = await client.PostAsJsonAsync("/api/v1/sales/invoices", new
        {
            customerId,
            invoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
            notes = (string?)null,
            lines = new[] { new { description = "Item", quantity = 1m, unitOfMeasure = (string?)null, unitPrice = amount, taxCodeId = (Guid?)null } },
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<Envelope<IdResponse>>(AuthHelper.JsonOptions);
        var issueResponse = await client.PostAsync($"/api/v1/sales/invoices/{created!.Data.Id}/issue", null);
        issueResponse.EnsureSuccessStatusCode();
        return created.Data.Id;
    }

    [Fact]
    public async Task CreateInvoice_PushingCustomerOverCreditLimit_StillSucceedsButReturnsWarning()
    {
        var client = await factory.AuthenticatedClientAsync("superadmin@litxus.demo");
        var customerId = await CreateCustomerAsync(client, creditLimit: 1000m);
        await CreateAndIssueInvoiceAsync(client, customerId, amount: 800m);

        var response = await client.PostAsJsonAsync("/api/v1/sales/invoices", new
        {
            customerId,
            invoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
            notes = (string?)null,
            lines = new[] { new { description = "Item", quantity = 1m, unitOfMeasure = (string?)null, unitPrice = 300m, taxCodeId = (Guid?)null } },
        });

        response.EnsureSuccessStatusCode(); // never blocked, even over limit
        var body = await response.Content.ReadFromJsonAsync<Envelope<IdResponse>>(AuthHelper.JsonOptions);
        body!.Meta!.CreditLimitWarning.Should().NotBeNullOrEmpty();
        body.Meta.CreditLimitWarning.Should().Contain("1,100.00").And.Contain("1,000.00");
    }

    [Fact]
    public async Task CreateInvoice_WithinCreditLimit_ReturnsNoWarning()
    {
        var client = await factory.AuthenticatedClientAsync("superadmin@litxus.demo");
        var customerId = await CreateCustomerAsync(client, creditLimit: 1000m);
        await CreateAndIssueInvoiceAsync(client, customerId, amount: 400m);

        var response = await client.PostAsJsonAsync("/api/v1/sales/invoices", new
        {
            customerId,
            invoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
            notes = (string?)null,
            lines = new[] { new { description = "Item", quantity = 1m, unitOfMeasure = (string?)null, unitPrice = 300m, taxCodeId = (Guid?)null } },
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Envelope<IdResponse>>(AuthHelper.JsonOptions);
        body!.Meta!.CreditLimitWarning.Should().BeNull();
    }

    [Fact]
    public async Task CreateInvoice_WhenCustomerHasNoCreditLimitConfigured_ReturnsNoWarningRegardlessOfAmount()
    {
        var client = await factory.AuthenticatedClientAsync("superadmin@litxus.demo");
        var customerId = await CreateCustomerAsync(client, creditLimit: 0m);

        var response = await client.PostAsJsonAsync("/api/v1/sales/invoices", new
        {
            customerId,
            invoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
            notes = (string?)null,
            lines = new[] { new { description = "Item", quantity = 1m, unitOfMeasure = (string?)null, unitPrice = 999999m, taxCodeId = (Guid?)null } },
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Envelope<IdResponse>>(AuthHelper.JsonOptions);
        body!.Meta!.CreditLimitWarning.Should().BeNull();
    }
}
