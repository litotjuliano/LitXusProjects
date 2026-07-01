using FluentValidation;

namespace LitXus.Application.Modules.Accounting.Commands.CreateGLEntry;

public class CreateGLEntryValidator : AbstractValidator<CreateGLEntryCommand>
{
    public CreateGLEntryValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty();

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId).NotEmpty();
            line.RuleFor(l => l)
                .Must(l => l.DebitAmount == 0 || l.CreditAmount == 0)
                .WithMessage("A line cannot have both a debit and a credit amount.");
            line.RuleFor(l => l.DebitAmount).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.CreditAmount).GreaterThanOrEqualTo(0);
        });
    }
}
