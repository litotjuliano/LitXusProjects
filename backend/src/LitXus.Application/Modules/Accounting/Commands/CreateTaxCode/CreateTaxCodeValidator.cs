using FluentValidation;
using LitXus.Domain.Modules.Accounting.Enums;

namespace LitXus.Application.Modules.Accounting.Commands.CreateTaxCode;

public class CreateTaxCodeValidator : AbstractValidator<CreateTaxCodeCommand>
{
    public CreateTaxCodeValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Rate).GreaterThanOrEqualTo(0).LessThanOrEqualTo(100);
        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(t => Enum.TryParse<TaxType>(t, out _))
            .WithMessage("Type must be one of: Sst, IncomeTax.");
    }
}
