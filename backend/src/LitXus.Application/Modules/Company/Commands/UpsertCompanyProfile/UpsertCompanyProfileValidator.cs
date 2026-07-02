using FluentValidation;
using LitXus.Domain.Modules.Shared.Enums;

namespace LitXus.Application.Modules.Company.Commands.UpsertCompanyProfile;

public class UpsertCompanyProfileValidator : AbstractValidator<UpsertCompanyProfileCommand>
{
    public UpsertCompanyProfileValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SsmRegistrationNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Tin).NotEmpty().MaximumLength(20);
        RuleFor(x => x.BusinessType)
            .Must(t => Enum.TryParse<BusinessType>(t, out _))
            .WithMessage("BusinessType must be one of: PrivateCompany, PublicCompany, SoleProprietor, Partnership, Other.");
        RuleFor(x => x.MsicCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.PrincipalBusinessActivity).NotEmpty().MaximumLength(500);
        RuleFor(x => x.FinancialYearEndMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.FinancialYearEndDay).InclusiveBetween(1, 31);
        RuleFor(x => x.AccountingFramework)
            .Must(t => Enum.TryParse<AccountingFramework>(t, out _))
            .WithMessage("AccountingFramework must be one of: Mpers, Mfrs.");
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
    }
}
