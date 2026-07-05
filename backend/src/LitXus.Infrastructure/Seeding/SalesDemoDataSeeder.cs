using LitXus.Application.Common.Interfaces;
using LitXus.Domain.Modules.Sales.Entities;
using LitXus.Domain.Modules.Sales.Enums;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Infrastructure.Seeding;

/// <summary>
/// Order 6 — runs after AccountingDemoDataSeeder (Order 5) since it points SalesSettings at
/// accounts that seeder already created (1010 Cash - Maybank, 1030 Accounts Receivable, 2200 SST
/// Payable, 4010 Sales Revenue). Issuing an invoice and verifying a payment here go through the
/// same domain methods (Invoice.Issue/Payment.Verify) the real API uses, so DomainEventDispatchInterceptor
/// auto-posts real GL entries for this seed data too — not a shortcut, the same pipeline as production.
/// </summary>
public class SalesDemoDataSeeder(IAppDbContext db, INumberSequenceGenerator numberSequenceGenerator, IDateTimeProvider dateTimeProvider) : ISeeder
{
    public int Order => 6;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await ConfigureSalesSettingsAsync(cancellationToken);
        var customers = await SeedCustomersAsync(cancellationToken);

        if (await db.Invoices.AnyAsync(cancellationToken))
        {
            return;
        }

        await SeedInvoicesPaymentsAndCreditNotesAsync(customers, cancellationToken);
    }

    private async Task ConfigureSalesSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await db.SalesSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is not null && settings.IsConfigured)
        {
            return;
        }

        var accounts = await db.Accounts
            .Where(a => new[] { "1010", "1030", "2200", "4010" }.Contains(a.Code))
            .ToDictionaryAsync(a => a.Code, cancellationToken);

        if (!accounts.ContainsKey("1010") || !accounts.ContainsKey("1030") || !accounts.ContainsKey("2200") || !accounts.ContainsKey("4010"))
        {
            return; // Accounting demo accounts not seeded (e.g. Accounting module absent) — leave unconfigured.
        }

        if (settings is null)
        {
            settings = SalesSettings.CreateEmpty();
            db.SalesSettings.Add(settings);
        }

        settings.Configure(accounts["1030"].Id, accounts["4010"].Id, accounts["2200"].Id, accounts["1010"].Id);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static readonly string[] CustomerNames =
    [
        "Tropikal Hardware Sdn Bhd", "Selangor Pipe Supplies Sdn Bhd", "KL Trading Enterprise",
        "Penang Distributors Sdn Bhd", "Johor Bahru Builders Supply", "Ipoh Steel & Metal Works",
        "Melaka Home Improvement Sdn Bhd", "Kuching Timber Trading", "Kota Kinabalu Hardware Hub",
        "Shah Alam Industrial Supplies", "Klang Valley Fasteners Sdn Bhd", "Seremban Plumbing Supplies",
        "Alor Setar Construction Materials", "Kuantan Coastal Trading Sdn Bhd", "Batu Pahat Tools & Equipment",
        "Petaling Jaya Retail Group", "Subang Jaya Building Merchants", "Cyberjaya Tech Supplies Sdn Bhd",
        "Putrajaya Office Solutions", "Bangi Agricultural Supplies", "Rawang Warehouse Trading",
        "Nilai Logistics & Supply Sdn Bhd", "Sungai Petani Hardware Mart", "Taiping Industrial Trading",
        "Sandakan Timber & Hardware", "Miri Oilfield Supplies Sdn Bhd", "Bintulu Marine Supplies",
        "Kluang Agro Trading Enterprise", "Muar Furniture & Fittings Sdn Bhd", "Tawau Import Export Trading",
        "Kajang Building Supplies Hub", "Ampang Hardware & Sanitary", "Cheras Trading Enterprise",
        "Gombak Industrial Fasteners", "Puchong Wholesale Trading Sdn Bhd", "Bukit Mertajam Steel Supply",
        "Kangar Northern Trading Sdn Bhd", "Kuala Terengganu Marine Hardware", "Kota Bharu Building Materials",
        "Labuan Offshore Supplies Sdn Bhd", "Sepang Aviation Support Trading",
    ];

    private async Task<List<Customer>> SeedCustomersAsync(CancellationToken cancellationToken)
    {
        if (await db.Customers.AnyAsync(cancellationToken))
        {
            return await db.Customers.ToListAsync(cancellationToken);
        }

        var customers = new List<Customer>();
        for (var i = 0; i < CustomerNames.Length; i++)
        {
            var code = $"CUST-{i + 1:D3}";
            var creditLimit = 10000m + (i % 5) * 5000m;
            var terms = i % 3 == 0 ? 60 : 30;
            var customer = Customer.Create(
                code, CustomerNames[i],
                contactPerson: $"Contact Person {i + 1}",
                email: $"accounts{i + 1}@{code.ToLowerInvariant()}.com.my",
                phone: $"+60 3-{7000 + i:D4} {1000 + i:D4}",
                address: "Malaysia",
                creditLimit: creditLimit,
                paymentTermsDays: terms);
            customers.Add(customer);
        }

        db.Customers.AddRange(customers);
        await db.SaveChangesAsync(cancellationToken);
        return customers;
    }

    private async Task SeedInvoicesPaymentsAndCreditNotesAsync(List<Customer> customers, CancellationToken cancellationToken)
    {
        var sst6 = await db.TaxCodes.FirstOrDefaultAsync(t => t.Code == "SST-6", cancellationToken);
        var year = dateTimeProvider.UtcNow.Year;
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        var invoiceIndex = 0;
        var createdInvoices = new List<Invoice>();

        for (var m = 1; m <= 6; m++)
        {
            for (var n = 0; n < 4 && invoiceIndex < 25; n++, invoiceIndex++)
            {
                var customer = customers[invoiceIndex % customers.Count];
                var invoiceDate = new DateOnly(year, m, Math.Min(5 + n * 6, DateTime.DaysInMonth(year, m)));
                var dueDate = invoiceDate.AddDays(customer.PaymentTermsDays);

                var qty = 10 + invoiceIndex % 15;
                var unitPrice = 50m + invoiceIndex % 20 * 12.5m;
                var line = InvoiceLine.Create($"PVC Pipe {4 + invoiceIndex % 3}in", qty, "pcs", unitPrice, sst6);

                var invoice = Invoice.CreateDraft(customer.Id, invoiceDate, dueDate, null, [line]);
                db.Invoices.Add(invoice);
                createdInvoices.Add(invoice);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        // Leave the last 2 as genuine Drafts (never issued) — everything else gets issued so the
        // narrative has real Issued/PartiallyPaid/Paid/Overdue/Void invoices to demonstrate.
        var toIssue = createdInvoices.Take(createdInvoices.Count - 2).ToList();
        foreach (var invoice in toIssue)
        {
            var invoiceNumber = await numberSequenceGenerator.NextInvoiceNumberAsync(cancellationToken);
            invoice.Issue(invoiceNumber);
        }

        await db.SaveChangesAsync(cancellationToken);

        // Payments: verify most, leave a couple Pending, reject one.
        var paymentTargets = toIssue.Take(18).ToList();
        for (var i = 0; i < paymentTargets.Count; i++)
        {
            var invoice = paymentTargets[i];
            var payAmount = i % 4 == 0 ? invoice.OutstandingBalance / 2 : invoice.OutstandingBalance;
            var payment = Payment.Create(invoice.Id, invoice.InvoiceDate.AddDays(5), payAmount, PaymentMethod.BankTransfer, $"REF-{2000 + i}", null);
            db.Payments.Add(payment);

            if (i < 15)
            {
                payment.Verify(Guid.Empty, dateTimeProvider.UtcNow);
                invoice.ApplyPayment(payAmount);
            }
            else if (i == 15)
            {
                payment.Reject("Bank reference could not be traced to any incoming transfer.");
            }
            // else leave Pending
        }

        await db.SaveChangesAsync(cancellationToken);

        // A couple of credit notes against fully-paid invoices with room left (there won't be —
        // apply against a partially-paid one's remaining balance instead).
        var creditNoteTargets = toIssue.Where(i => i.OutstandingBalance > 0).Take(2).ToList();
        foreach (var invoice in creditNoteTargets)
        {
            var creditAmount = Math.Min(50m, invoice.OutstandingBalance);
            if (creditAmount <= 0) continue;

            var creditNoteNumber = await numberSequenceGenerator.NextCreditNoteNumberAsync(cancellationToken);
            var creditNote = CreditNote.Create(creditNoteNumber, invoice.Id, "Minor quantity discrepancy on delivery.", creditAmount);
            invoice.ApplyPayment(creditAmount);
            db.CreditNotes.Add(creditNote);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
