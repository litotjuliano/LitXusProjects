using FluentValidation;

namespace LitXus.Application.Modules.Accounting.Commands.UpdateAccount;

public class UpdateAccountValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
