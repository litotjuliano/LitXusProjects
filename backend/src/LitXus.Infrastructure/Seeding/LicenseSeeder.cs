using LitXus.Application.Common.Interfaces;
using LitXus.Domain.Modules.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Infrastructure.Seeding;

/// <summary>Seeds a local/demo Accounting Pro license — see docs/16_Feature_Flags.md.</summary>
public class LicenseSeeder(IAppDbContext db, IDateTimeProvider dateTimeProvider) : ISeeder
{
    public int Order => 2;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await db.Licenses.AnyAsync(cancellationToken))
        {
            return;
        }

        var license = License.Create(
            productCode: "AccountingPro",
            enabledModules: "Accounting",
            issuedToCompany: "LitXus Systems (Local Dev)",
            issuedAtUtc: dateTimeProvider.UtcNow,
            expiresAtUtc: dateTimeProvider.UtcNow.AddYears(1),
            licenseKey: "dev-local-accounting-pro");

        db.Licenses.Add(license);
        await db.SaveChangesAsync(cancellationToken);
    }
}
