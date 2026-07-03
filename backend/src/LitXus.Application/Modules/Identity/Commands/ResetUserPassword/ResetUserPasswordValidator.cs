using FluentValidation;

namespace LitXus.Application.Modules.Identity.Commands.ResetUserPassword;

public class ResetUserPasswordValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(10);
    }
}
