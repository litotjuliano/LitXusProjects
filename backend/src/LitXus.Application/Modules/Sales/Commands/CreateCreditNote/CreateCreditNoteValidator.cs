using FluentValidation;

namespace LitXus.Application.Modules.Sales.Commands.CreateCreditNote;

public class CreateCreditNoteValidator : AbstractValidator<CreateCreditNoteCommand>
{
    public CreateCreditNoteValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
