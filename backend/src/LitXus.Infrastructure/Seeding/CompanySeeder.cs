using LitXus.Application.Common.Interfaces;
using LitXus.Domain.Modules.Shared.Entities;
using LitXus.Domain.Modules.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Infrastructure.Seeding;

/// <summary>Seeds a demo Malaysian SMB company profile — see docs/15_Malaysia_Compliance.md.</summary>
public class CompanySeeder(IAppDbContext db) : ISeeder
{
    public int Order => 2;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await db.Companies.AnyAsync(cancellationToken))
        {
            return;
        }

        var fields = new CompanyProfileFields(
            Name: "LitXus Demo Trading Sdn Bhd",
            SsmRegistrationNumber: "202601012345",
            Tin: "C12345678090",
            Usid: null,
            BusinessRegistrationNumber: null,
            BusinessType: BusinessType.PrivateCompany,
            MsicCode: "4799",
            PrincipalBusinessActivity: "Other specialized wholesale trading",
            EstablishmentDate: new DateOnly(2020, 1, 15),
            FinancialYearEndMonth: 12,
            FinancialYearEndDay: 31,
            AccountingFramework: AccountingFramework.Mpers,
            AddressLine1: "No. 12, Jalan Industri 2/3",
            AddressLine2: "Seksyen 15",
            City: "Shah Alam",
            State: "Selangor",
            PostalCode: "40200",
            Country: "Malaysia",
            Phone: "+603-5510 1234",
            SecondaryPhone: null,
            Email: "admin@litxus.demo",
            Website: null,
            PrimaryBankName: "Maybank",
            PrimaryBankAccountNumber: "5141 2345 6789",
            PrimaryBankAccountHolderName: "LitXus Demo Trading Sdn Bhd",
            PrimaryBankSwiftCode: "MBBEMYKL",
            SstRegistrationNumber: "W10-1808-32000123",
            EisNumber: null,
            EpfNumber: null,
            SocsoNumber: null,
            ExternalAuditorName: null,
            CompanySecretaryName: null);

        db.Companies.Add(Company.Create(fields));
        await db.SaveChangesAsync(cancellationToken);
    }
}
