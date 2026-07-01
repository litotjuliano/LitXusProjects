using FluentValidation;

namespace LitXus.Application.Modules.Accounting.Commands.VoidGLEntry;

public class VoidGLEntryValidator : AbstractValidator<VoidGLEntryCommand>
{
    public VoidGLEntryValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A reason is required to void a GL entry.");
    }
}
