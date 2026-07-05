using FluentValidation;

namespace LitXus.Application.Modules.Sales.Commands.UpdateInvoice;

public class UpdateInvoiceValidator : AbstractValidator<UpdateInvoiceCommand>
{
    public UpdateInvoiceValidator()
    {
        RuleFor(x => x.DueDate).GreaterThanOrEqualTo(x => x.InvoiceDate)
            .WithMessage("Due date cannot be before the invoice date.");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("An invoice needs at least 1 line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Description).NotEmpty().MaximumLength(500);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}
