using LitXus.Domain.Modules.Shared.Entities;

namespace LitXus.Application.Modules.Company.Dtos;

public record CompanyDto(
    Guid Id,
    string Name,
    string SsmRegistrationNumber,
    string Tin,
    string? Usid,
    string? BusinessRegistrationNumber,
    string BusinessType,
    string MsicCode,
    string PrincipalBusinessActivity,
    DateOnly? EstablishmentDate,
    int FinancialYearEndMonth,
    int FinancialYearEndDay,
    string AccountingFramework,
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

public record CompanySignatoryDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string IcNumber,
    string Position,
    string Email,
    string? Phone);

public static class CompanyMappingExtensions
{
    public static CompanyDto ToDto(this Domain.Modules.Shared.Entities.Company company) => new(
        company.Id,
        company.Name,
        company.SsmRegistrationNumber,
        company.Tin,
        company.Usid,
        company.BusinessRegistrationNumber,
        company.BusinessType.ToString(),
        company.MsicCode,
        company.PrincipalBusinessActivity,
        company.EstablishmentDate,
        company.FinancialYearEndMonth,
        company.FinancialYearEndDay,
        company.AccountingFramework.ToString(),
        company.AddressLine1,
        company.AddressLine2,
        company.City,
        company.State,
        company.PostalCode,
        company.Country,
        company.Phone,
        company.SecondaryPhone,
        company.Email,
        company.Website,
        company.PrimaryBankName,
        company.PrimaryBankAccountNumber,
        company.PrimaryBankAccountHolderName,
        company.PrimaryBankSwiftCode,
        company.SstRegistrationNumber,
        company.EisNumber,
        company.EpfNumber,
        company.SocsoNumber,
        company.ExternalAuditorName,
        company.CompanySecretaryName);

    public static CompanySignatoryDto ToDto(this CompanySignatory signatory) => new(
        signatory.Id,
        signatory.CompanyId,
        signatory.Name,
        signatory.IcNumber,
        signatory.Position,
        signatory.Email,
        signatory.Phone);
}
