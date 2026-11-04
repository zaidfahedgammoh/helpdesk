using FluentValidation;
namespace HelpDeskLive.Application.Tickets.CreateTicket;
public sealed class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.IssueTypeId).GreaterThan(0).When(x => x.IssueTypeId.HasValue);
        RuleFor(x => x.CustomTitle).MaximumLength(200);
        RuleFor(x => x).Must(x =>
            (x.IssueTypeId.HasValue && string.IsNullOrWhiteSpace(x.CustomTitle)) ||
            (!x.IssueTypeId.HasValue && !string.IsNullOrWhiteSpace(x.CustomTitle)))
            .WithMessage("Choose one issue type or provide a custom title, not both.");
        RuleFor(x => x.CustomTitle).NotEmpty().When(x => !x.IssueTypeId.HasValue);
    }
}