using LitXus.Application.Modules.Company.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Company.Commands.UpsertCompanyProfile;

public record UpsertCompanyProfileCommand(
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
    string? CompanySecretaryName) : IRequest<CompanyDto>;
