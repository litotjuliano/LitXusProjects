using LitXus.Application.Common.Interfaces;
using LitXus.Domain.Modules.Accounting.Entities;
using LitXus.Domain.Modules.Accounting.Enums;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Infrastructure.Seeding;

/// <summary>
/// A realistic Malaysia SME distributor scenario spanning 6 months (Jan-Jun) — 28 accounts and
/// ~86 GL entry rows a user can actually click through in the app: recurring rent/salaries/
/// utilities, two ongoing customers (one that pays fast, one that runs an aging balance), SST on
/// every sale, an equipment and vehicle purchase (the latter partly loan-funded), insurance
/// amortization, straight-line-ish depreciation, plus the specific edge cases
/// docs/phase-1-accounting/Test_Scenarios.md calls for: balanced and intentionally-unbalanced
/// Draft entries, Voided entries with real reasons, a zero-value balanced entry, and a
/// future-dated entry. Checks each Account.Code individually (not "any accounts exist") so it
/// layers safely on top of accounts a user already created by hand.
/// </summary>
public class AccountingDemoDataSeeder(
    IAppDbContext db,
    INumberSequenceGenerator numberSequenceGenerator,
    IDateTimeProvider dateTimeProvider) : ISeeder
{
    public int Order => 4;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var accounts = await SeedAccountsAsync(cancellationToken);
        await SeedTaxCodesAsync(cancellationToken);

        if (await db.GLEntries.AnyAsync(cancellationToken))
        {
            return;
        }

        await SeedGLEntriesAsync(accounts, cancellationToken);
    }

    private async Task<Dictionary<string, Account>> SeedAccountsAsync(CancellationToken cancellationToken)
    {
        var definitions = new (string Code, string Name, AccountType Type)[]
        {
            ("1010", "Cash - Maybank Current", AccountType.Asset),
            ("1015", "Petty Cash", AccountType.Asset),
            ("1020", "Cash - CIMB Savings", AccountType.Asset),
            ("1030", "Accounts Receivable", AccountType.Asset),
            ("1050", "Prepaid Expenses", AccountType.Asset),
            ("1110", "Office Equipment", AccountType.Asset),
            ("1120", "Motor Vehicle", AccountType.Asset),

            ("2010", "Accounts Payable", AccountType.Liability),
            ("2100", "Accrued Liabilities", AccountType.Liability),
            ("2200", "SST Payable", AccountType.Liability),
            ("2300", "Loan Payable - Bank", AccountType.Liability),

            ("3010", "Share Capital", AccountType.Equity),
            ("3020", "Retained Earnings", AccountType.Equity),

            ("4010", "Sales Revenue", AccountType.Revenue),
            ("4020", "Service Revenue", AccountType.Revenue),
            ("4030", "Other Income", AccountType.Revenue),

            ("5100", "Rent Expense", AccountType.Expense),
            ("5110", "Utilities Expense", AccountType.Expense),
            ("5120", "Salaries Expense", AccountType.Expense),
            ("5130", "Office Supplies Expense", AccountType.Expense),
            ("5140", "Bank Charges", AccountType.Expense),
            ("5150", "Marketing Expense", AccountType.Expense),
            ("5160", "Insurance Expense", AccountType.Expense),
            ("5170", "Vehicle & Transport Expense", AccountType.Expense),
            ("5180", "Professional Fees", AccountType.Expense),
            ("5190", "Depreciation Expense", AccountType.Expense),
            ("5200", "Interest Expense", AccountType.Expense),
            ("5210", "Miscellaneous Expense", AccountType.Expense),
        };

        var existing = await db.Accounts.ToDictionaryAsync(a => a.Code, cancellationToken);
        var created = false;

        foreach (var (code, name, type) in definitions)
        {
            if (existing.ContainsKey(code))
            {
                continue;
            }

            var account = Account.Create(code, name, type, null);
            db.Accounts.Add(account);
            existing[code] = account;
            created = true;
        }

        if (created)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return existing;
    }

    private async Task SeedTaxCodesAsync(CancellationToken cancellationToken)
    {
        if (await db.TaxCodes.AnyAsync(cancellationToken))
        {
            return;
        }

        db.TaxCodes.Add(TaxCode.Create("SST-6", "Sales & Service Tax 6%", 6.00m, TaxType.Sst));
        db.TaxCodes.Add(TaxCode.Create("SST-0", "Zero-rated / exempt", 0.00m, TaxType.Sst));
        await db.SaveChangesAsync(cancellationToken);
    }

    private record Line(string Code, decimal Debit, decimal Credit);
    private record Txn(DateOnly Date, string Description, Line[] Lines);

    private async Task SeedGLEntriesAsync(Dictionary<string, Account> accounts, CancellationToken cancellationToken)
    {
        Account A(string code) => accounts[code];
        var year = dateTimeProvider.UtcNow.Year;
        var monthNames = new[] { "", "January", "February", "March", "April", "May", "June" };

        var posted = new List<Txn>();

        // --- One-off narrative transactions ---
        posted.Add(new(new DateOnly(year, 1, 2), "Initial capital injection",
            [new("1010", 50000, 0), new("3010", 0, 50000)]));
        posted.Add(new(new DateOnly(year, 1, 3), "Petty cash float top-up",
            [new("1015", 500, 0), new("1010", 0, 500)]));
        posted.Add(new(new DateOnly(year, 1, 6), "Office equipment purchase - laptops and furniture",
            [new("1110", 8500, 0), new("1010", 0, 8500)]));
        posted.Add(new(new DateOnly(year, 2, 10), "Motor vehicle purchase - delivery van (partly loan-funded)",
            [new("1120", 65000, 0), new("1010", 0, 15000), new("2300", 0, 50000)]));
        posted.Add(new(new DateOnly(year, 3, 3), "Annual insurance premium prepaid",
            [new("1050", 4800, 0), new("1010", 0, 4800)]));
        posted.Add(new(new DateOnly(year, 6, 15), "Professional fees - annual audit",
            [new("5180", 3500, 0), new("1010", 0, 3500)]));

        // --- Monthly recurring cycle, Jan-Jun ---
        for (var m = 1; m <= 6; m++)
        {
            var name = monthNames[m];

            posted.Add(new(new DateOnly(year, m, 5), $"{name} office rent - Shah Alam",
                [new("5100", 3500, 0), new("1010", 0, 3500)]));
            posted.Add(new(new DateOnly(year, m, 28), $"{name} staff salaries",
                [new("5120", 8500, 0), new("1010", 0, 8500)]));
            posted.Add(new(new DateOnly(year, m, DateTime.DaysInMonth(year, m)), $"TNB electricity bill - {name}",
                [new("5110", 380 + m * 10, 0), new("1010", 0, 380 + m * 10)]));
            posted.Add(new(new DateOnly(year, m, 20), "Bank charges - monthly account fee",
                [new("5140", 25, 0), new("1010", 0, 25)]));
            posted.Add(new(new DateOnly(year, m, 25), "Office supplies - stationery and consumables",
                [new("5130", 200 + m * 15, 0), new("1010", 0, 200 + m * 15)]));
            posted.Add(new(new DateOnly(year, m, 26), "Sundry expenses (petty cash)",
                [new("5210", 55 + m * 3, 0), new("1015", 0, 55 + m * 3)]));
            posted.Add(new(new DateOnly(year, m, 27), "Interest income - CIMB savings",
                [new("1020", 90 + m * 5, 0), new("4030", 0, 90 + m * 5)]));
            posted.Add(new(new DateOnly(year, m, 15), "Insurance amortization",
                [new("5160", 400, 0), new("1050", 0, 400)]));
            // Day 28, not month-end, so this is safe for February too (never fewer than 28 days).
            posted.Add(new(new DateOnly(year, m, 28), "Depreciation - equipment and vehicle",
                [new("5190", 200, 0), new("1110", 0, 100), new("1120", 0, 100)]));

            // Tropikal Hardware Sdn Bhd — invoiced monthly, never paid down in this dataset
            // (an intentionally aging receivable, useful later for AR-aging testing in Phase 2).
            var tropikalSubtotal = 4500 + m * 100m;
            var tropikalSst = Math.Round(tropikalSubtotal * 0.06m, 2);
            posted.Add(new(new DateOnly(year, m, 8), "Sales invoice - Tropikal Hardware Sdn Bhd",
                [new("1030", tropikalSubtotal + tropikalSst, 0), new("4010", 0, tropikalSubtotal), new("2200", 0, tropikalSst)]));

            // Selangor Pipe Supplies Sdn Bhd — invoiced and paid within the same month.
            var selangorSubtotal = 2800 + m * 50m;
            var selangorSst = Math.Round(selangorSubtotal * 0.06m, 2);
            var selangorTotal = selangorSubtotal + selangorSst;
            posted.Add(new(new DateOnly(year, m, 12), "Sales invoice - Selangor Pipe Supplies Sdn Bhd",
                [new("1030", selangorTotal, 0), new("4010", 0, selangorSubtotal), new("2200", 0, selangorSst)]));
            posted.Add(new(new DateOnly(year, m, 19), "Customer payment received - Selangor Pipe Supplies",
                [new("1010", selangorTotal, 0), new("1030", 0, selangorTotal)]));

            var serviceAmount = 6000 + m * 250m;
            posted.Add(new(new DateOnly(year, m, 22), "Service revenue - consulting/retainer client",
                [new("1010", serviceAmount, 0), new("4020", 0, serviceAmount)]));

            if (m % 2 == 0)
            {
                posted.Add(new(new DateOnly(year, m, 14), "Marketing - social media advertising",
                    [new("5150", 600, 0), new("1010", 0, 600)]));
            }

            if (m >= 3)
            {
                posted.Add(new(new DateOnly(year, m, 28), $"Loan interest - {name}",
                    [new("5200", 417, 0), new("1010", 0, 417)]));
            }

            if (m % 3 == 0)
            {
                posted.Add(new(new DateOnly(year, m, 17), "Vehicle - fuel and maintenance",
                    [new("5170", 350, 0), new("1010", 0, 350)]));
            }
        }

        foreach (var t in posted.OrderBy(t => t.Date))
        {
            await PostEntryAsync(A, t, cancellationToken);
        }

        // --- Edge cases from docs/phase-1-accounting/Test_Scenarios.md ---

        // Draft entries — never posted. Two are internally balanced (realistic "pending review"
        // entries); two are deliberately unbalanced so the posting-validation UI has something
        // real to reject without corrupting the actual ledger.
        CreateDraft(A, new DateOnly(year, 7, 1), "Draft - July office rent (pending approval)",
            [new("5100", 3500, 0), new("1010", 0, 3500)]);
        CreateDraft(A, new DateOnly(year, 6, 20), "Draft - equipment maintenance contract",
            [new("5170", 800, 0), new("1010", 0, 800)]);
        CreateDraft(A, new DateOnly(year, 6, 18), "Draft - data entry error example (intentionally unbalanced)",
            [new("1110", 2000, 0), new("1010", 0, 1800)]);
        CreateDraft(A, new DateOnly(year, 6, 21), "Draft - incomplete entry example (intentionally unbalanced)",
            [new("5130", 500, 0), new("1010", 0, 450)]);
        await db.SaveChangesAsync(cancellationToken);

        // Voided entries — posted, then voided with a real reason, per docs/07_Audit_Trail.md.
        var voidReasons = new (DateOnly Date, string Description, Line[] Lines, string Reason)[]
        {
            (new DateOnly(year, 1, 20), "Duplicate rent entry", [new("5100", 3500, 0), new("1010", 0, 3500)],
                "Duplicate entry — see the actual January rent payment entry for the real transaction."),
            (new DateOnly(year, 2, 18), "Office supplies miscoded", [new("5130", 300, 0), new("1010", 0, 300)],
                "Posted to the wrong expense account; reversed and re-entered correctly."),
            (new DateOnly(year, 3, 15), "Test entry during system setup", [new("1010", 100, 0), new("4020", 0, 100)],
                "Accidental test entry created while setting up the system, not a real transaction."),
            (new DateOnly(year, 4, 22), "Overstated utilities bill", [new("5110", 500, 0), new("1010", 0, 500)],
                "Amount was entered incorrectly; corrected in a new entry for the right figure."),
            (new DateOnly(year, 5, 10), "Cancelled marketing spend", [new("5150", 600, 0), new("1010", 0, 600)],
                "Campaign was cancelled before payment was actually made."),
        };
        foreach (var (date, description, lines, reason) in voidReasons)
        {
            var entry = await PostEntryAsync(A, new Txn(date, description, lines), cancellationToken);
            entry.Void(reason);
        }
        await db.SaveChangesAsync(cancellationToken);

        // Zero-value entry — technically balanced (0 = 0), must still post successfully.
        await PostEntryAsync(A, new Txn(new DateOnly(year, 6, 30), "Zero-value test entry (edge case)",
            [new("1010", 0, 0), new("4020", 0, 0)]), cancellationToken);

        // Future-dated entry — Phase 1 has no period-close, so this is accepted, not rejected.
        await PostEntryAsync(A, new Txn(new DateOnly(year, 9, 15), "Advance rental payment for September (prepaid)",
            [new("5100", 3500, 0), new("1010", 0, 3500)]), cancellationToken);
    }

    private void CreateDraft(Func<string, Account> a, DateOnly date, string description, Line[] lines)
    {
        var glLines = lines.Select(l => GLEntryLine.Create(a(l.Code), l.Debit, l.Credit, null)).ToList();
        db.GLEntries.Add(GLEntry.CreateDraft(date, description, glLines));
    }

    private async Task<GLEntry> PostEntryAsync(Func<string, Account> a, Txn t, CancellationToken cancellationToken)
    {
        var glLines = t.Lines.Select(l => GLEntryLine.Create(a(l.Code), l.Debit, l.Credit, null)).ToList();
        var entry = GLEntry.CreateDraft(t.Date, t.Description, glLines);
        db.GLEntries.Add(entry);

        var entryNumber = await numberSequenceGenerator.NextGLEntryNumberAsync(cancellationToken);
        entry.Post(entryNumber, Guid.Empty, dateTimeProvider.UtcNow);
        return entry;
    }
}
