using FluentValidation;

namespace LitXus.Application.Modules.Licensing.Commands.ApplyLicenseKey;

public class ApplyLicenseKeyValidator : AbstractValidator<ApplyLicenseKeyCommand>
{
    public ApplyLicenseKeyValidator()
    {
        RuleFor(x => x.LicenseKey).NotEmpty().WithMessage("Please paste a license key.");
    }
}
