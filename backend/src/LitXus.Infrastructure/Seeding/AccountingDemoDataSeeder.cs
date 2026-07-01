using LitXus.Application.Common.Interfaces;
using LitXus.Domain.Modules.Accounting.Entities;
using LitXus.Domain.Modules.Accounting.Enums;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Infrastructure.Seeding;

/// <summary>
/// Modest demo Chart of Accounts + a handful of realistic posted GL entries, so the Reports
/// pages (Trial Balance, Income Statement, Balance Sheet, General Ledger) have real data to
/// show out of the box. Smaller than the 30-40 accounts / 100+ entries target in
/// docs/08_Sample_Data.md — that fuller dataset is still a follow-up, this is enough to prove
/// the reports are correct. Checks each account by Code individually (not "any accounts exist")
/// so it can layer on top of accounts a user already created by hand.
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
            ("1020", "Cash - CIMB Savings", AccountType.Asset),
            ("1030", "Accounts Receivable", AccountType.Asset),
            ("1050", "Prepaid Expenses", AccountType.Asset),
            ("1110", "Office Equipment", AccountType.Asset),
            ("2010", "Accounts Payable", AccountType.Liability),
            ("2100", "Accrued Liabilities", AccountType.Liability),
            ("2200", "SST Payable", AccountType.Liability),
            ("3010", "Share Capital", AccountType.Equity),
            ("4010", "Sales Revenue", AccountType.Revenue),
            ("4020", "Service Revenue", AccountType.Revenue),
            ("5100", "Rent Expense", AccountType.Expense),
            ("5110", "Utilities Expense", AccountType.Expense),
            ("5120", "Salaries Expense", AccountType.Expense),
            ("5130", "Office Supplies Expense", AccountType.Expense),
            ("5140", "Bank Charges", AccountType.Expense),
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

    private async Task SeedGLEntriesAsync(Dictionary<string, Account> accounts, CancellationToken cancellationToken)
    {
        Account A(string code) => accounts[code];

        var year = dateTimeProvider.UtcNow.Year;
        var transactions = new (int Month, int Day, string Description, (string Code, decimal Debit, decimal Credit)[] Lines)[]
        {
            (1, 2, "Initial capital injection", [("1010", 50000, 0), ("3010", 0, 50000)]),
            (1, 5, "January office rent - Shah Alam", [("5100", 3500, 0), ("1010", 0, 3500)]),
            (1, 28, "January staff salaries", [("5120", 8000, 0), ("1010", 0, 8000)]),
            (1, 31, "TNB electricity bill - January", [("5110", 420, 0), ("1010", 0, 420)]),
            (2, 3, "Office equipment purchase - laptops", [("1110", 6000, 0), ("1010", 0, 6000)]),
            (2, 5, "February office rent", [("5100", 3500, 0), ("1010", 0, 3500)]),
            (2, 15, "Service revenue - consulting engagement", [("1010", 12000, 0), ("4020", 0, 12000)]),
            (2, 28, "February staff salaries", [("5120", 8000, 0), ("1010", 0, 8000)]),
            (3, 5, "March office rent", [("5100", 3500, 0), ("1010", 0, 3500)]),
            (3, 10, "Sales invoice - Tropikal Hardware Sdn Bhd (with SST)", [("1030", 10600, 0), ("4010", 0, 10000), ("2200", 0, 600)]),
            (3, 20, "Office supplies - stationery and printer ink", [("5130", 350, 0), ("1010", 0, 350)]),
            (3, 31, "March staff salaries", [("5120", 8000, 0), ("1010", 0, 8000)]),
            (4, 2, "Customer payment received - Tropikal Hardware", [("1010", 10600, 0), ("1030", 0, 10600)]),
            (4, 5, "April office rent", [("5100", 3500, 0), ("1010", 0, 3500)]),
            (4, 18, "Service revenue - retainer client", [("1010", 9500, 0), ("4020", 0, 9500)]),
            (4, 30, "April staff salaries", [("5120", 8000, 0), ("1010", 0, 8000)]),
            (5, 5, "May office rent", [("5100", 3500, 0), ("1010", 0, 3500)]),
            (5, 12, "Sales invoice - Selangor Pipe Supplies Sdn Bhd (with SST)", [("1030", 5300, 0), ("4010", 0, 5000), ("2200", 0, 300)]),
            (5, 20, "Bank charges - monthly account fee", [("5140", 25, 0), ("1010", 0, 25)]),
            (5, 31, "May staff salaries", [("5120", 8000, 0), ("1010", 0, 8000)]),
        };

        var entries = transactions
            .Select(t => (Date: new DateOnly(year, t.Month, t.Day), t.Description, t.Lines))
            .OrderBy(t => t.Date)
            .ToList();

        foreach (var t in entries)
        {
            var lines = t.Lines.Select(l => GLEntryLine.Create(A(l.Code), l.Debit, l.Credit, null)).ToList();
            var entry = GLEntry.CreateDraft(t.Date, t.Description, lines);
            db.GLEntries.Add(entry);

            var entryNumber = await numberSequenceGenerator.NextGLEntryNumberAsync(cancellationToken);
            entry.Post(entryNumber, Guid.Empty, dateTimeProvider.UtcNow);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
