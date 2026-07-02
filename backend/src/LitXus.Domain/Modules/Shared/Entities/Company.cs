using LitXus.Domain.Common;
using LitXus.Domain.Modules.Shared.Enums;

namespace LitXus.Domain.Modules.Shared.Entities;

/// <summary>
/// One row per deployed instance — the tenant business using LitXus, distinct from the
/// LitXus product/brand itself. See docs/15_Malaysia_Compliance.md.
/// </summary>
public class Company : BaseEntity, IAuditable
{
    // Registration
    public string Name { get; private set; } = string.Empty;
    public string SsmRegistrationNumber { get; private set; } = string.Empty;
    public string Tin { get; private set; } = string.Empty;
    public string? Usid { get; private set; }
    public string? BusinessRegistrationNumber { get; private set; }

    // Business details
    public BusinessType BusinessType { get; private set; }
    public string MsicCode { get; private set; } = string.Empty;
    public string PrincipalBusinessActivity { get; private set; } = string.Empty;
    public DateOnly? EstablishmentDate { get; private set; }
    public int FinancialYearEndMonth { get; private set; } = 12;
    public int FinancialYearEndDay { get; private set; } = 31;
    public AccountingFramework AccountingFramework { get; private set; } = AccountingFramework.Mpers;

    // Address
    public string AddressLine1 { get; private set; } = string.Empty;
    public string? AddressLine2 { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string Country { get; private set; } = "Malaysia";

    // Contact
    public string Phone { get; private set; } = string.Empty;
    public string? SecondaryPhone { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string? Website { get; private set; }

    // Primary bank (letterhead/invoice display only — not the GL-reconciliation BankAccount entity)
    public string? PrimaryBankName { get; private set; }
    public string? PrimaryBankAccountNumber { get; private set; }
    public string? PrimaryBankAccountHolderName { get; private set; }
    public string? PrimaryBankSwiftCode { get; private set; }

    // Statutory numbers
    public string? SstRegistrationNumber { get; private set; }
    public string? EisNumber { get; private set; }
    public string? EpfNumber { get; private set; }
    public string? SocsoNumber { get; private set; }

    // Governance
    public string? ExternalAuditorName { get; private set; }
    public string? CompanySecretaryName { get; private set; }

    private Company() { }

    public static Company Create(CompanyProfileFields fields)
    {
        var company = new Company();
        company.UpdateProfile(fields);
        return company;
    }

    public void UpdateProfile(CompanyProfileFields fields)
    {
        Name = fields.Name;
        SsmRegistrationNumber = fields.SsmRegistrationNumber;
        Tin = fields.Tin;
        Usid = fields.Usid;
        BusinessRegistrationNumber = fields.BusinessRegistrationNumber;

        BusinessType = fields.BusinessType;
        MsicCode = fields.MsicCode;
        PrincipalBusinessActivity = fields.PrincipalBusinessActivity;
        EstablishmentDate = fields.EstablishmentDate;
        FinancialYearEndMonth = fields.FinancialYearEndMonth;
        FinancialYearEndDay = fields.FinancialYearEndDay;
        AccountingFramework = fields.AccountingFramework;

        AddressLine1 = fields.AddressLine1;
        AddressLine2 = fields.AddressLine2;
        City = fields.City;
        State = fields.State;
        PostalCode = fields.PostalCode;
        Country = fields.Country;

        Phone = fields.Phone;
        SecondaryPhone = fields.SecondaryPhone;
        Email = fields.Email;
        Website = fields.Website;

        PrimaryBankName = fields.PrimaryBankName;
        PrimaryBankAccountNumber = fields.PrimaryBankAccountNumber;
        PrimaryBankAccountHolderName = fields.PrimaryBankAccountHolderName;
        PrimaryBankSwiftCode = fields.PrimaryBankSwiftCode;

        SstRegistrationNumber = fields.SstRegistrationNumber;
        EisNumber = fields.EisNumber;
        EpfNumber = fields.EpfNumber;
        SocsoNumber = fields.SocsoNumber;

        ExternalAuditorName = fields.ExternalAuditorName;
        CompanySecretaryName = fields.CompanySecretaryName;
    }
}

/// <summary>Field bag shared by Company.Create/UpdateProfile so the constructor/updater don't drift out of sync.</summary>
public record CompanyProfileFields(
    string Name,
    string SsmRegistrationNumber,
    string Tin,
    string? Usid,
    string? BusinessRegistrationNumber,
    BusinessType BusinessType,
    string MsicCode,
    string PrincipalBusinessActivity,
    DateOnly? EstablishmentDate,
    int FinancialYearEndMonth,
    int FinancialYearEndDay,
    AccountingFramework AccountingFramework,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string Country,
    string Phone,
    string? SecondaryPhone,
    string Email,
    string? Website,
    string? PrimaryBankName,
    string? PrimaryBankAccountNumber,
    string? PrimaryBankAccountHolderName,
    string? PrimaryBankSwiftCode,
    string? SstRegistrationNumber,
    string? EisNumber,
    string? EpfNumber,
    string? SocsoNumber,
    string? ExternalAuditorName,
    string? CompanySecretaryName);
