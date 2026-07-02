using FluentValidation;

namespace LitXus.Application.Modules.Company.Commands.AddSignatory;

public class AddSignatoryValidator : AbstractValidator<AddSignatoryCommand>
{
    public AddSignatoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.IcNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Position).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
    }
}
