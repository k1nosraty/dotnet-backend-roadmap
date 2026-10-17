using FluentValidation;
namespace SupportDesk.Application;

public sealed class ResolveTicketValidator : AbstractValidator<ResolveTicketCommand>
{
    public ResolveTicketValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be positive.");
        RuleFor(x => x.Resolution).Must(text => text is not null && text.Trim().Length is >= 3 and <= 500)
            .WithMessage("Resolution must contain 3-500 characters after trim.");
    }
}
