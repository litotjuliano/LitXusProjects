using FluentValidation;
using LitXus.Domain.Modules.Sales.Enums;

namespace LitXus.Application.Modules.Sales.Commands.RecordPayment;

public class RecordPaymentValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Method)
            .NotEmpty()
            .Must(m => Enum.TryParse<PaymentMethod>(m, out _))
            .WithMessage("Method must be one of: BankTransfer, Cash, Cheque, OnlineGateway.");
    }
}
