using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Company.Dtos;
using LitXus.Domain.Modules.Shared.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CompanyEntity = LitXus.Domain.Modules.Shared.Entities.Company;
using CompanyProfileFields = LitXus.Domain.Modules.Shared.Entities.CompanyProfileFields;

namespace LitXus.Application.Modules.Company.Commands.UpsertCompanyProfile;

public class UpsertCompanyProfileCommandHandler(IAppDbContext db) : IRequestHandler<UpsertCompanyProfileCommand, CompanyDto>
{
    public async Task<CompanyDto> Handle(UpsertCompanyProfileCommand request, CancellationToken cancellationToken)
    {
        var fields = new CompanyProfileFields(
            request.Name,
            request.SsmRegistrationNumber,
            request.Tin,
            request.Usid,
            request.BusinessRegistrationNumber,
            Enum.Parse<BusinessType>(request.BusinessType),
            request.MsicCode,
            request.PrincipalBusinessActivity,
            request.EstablishmentDate,
            request.FinancialYearEndMonth,
            request.FinancialYearEndDay,
            Enum.Parse<AccountingFramework>(request.AccountingFramework),
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.State,
            request.PostalCode,
            request.Country,
            request.Phone,
            request.SecondaryPhone,
            request.Email,
            request.Website,
            request.PrimaryBankName,
            request.PrimaryBankAccountNumber,
            request.PrimaryBankAccountHolderName,
            request.PrimaryBankSwiftCode,
            request.SstRegistrationNumber,
            request.EisNumber,
            request.EpfNumber,
            request.SocsoNumber,
            request.ExternalAuditorName,
            request.CompanySecretaryName);

        var company = await db.Companies.FirstOrDefaultAsync(cancellationToken);
        if (company is null)
        {
            company = CompanyEntity.Create(fields);
            db.Companies.Add(company);
        }
        else
        {
            company.UpdateProfile(fields);
        }

        await db.SaveChangesAsync(cancellationToken);
        return company.ToDto();
    }
}
