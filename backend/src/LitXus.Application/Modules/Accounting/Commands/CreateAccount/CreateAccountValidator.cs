using FluentValidation;
using LitXus.Domain.Modules.Accounting.Enums;

namespace LitXus.Application.Modules.Accounting.Commands.CreateAccount;

public class CreateAccountValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(t => Enum.TryParse<AccountType>(t, out _))
            .WithMessage("Type must be one of: Asset, Liability, Equity, Revenue, Expense.");
    }
}
