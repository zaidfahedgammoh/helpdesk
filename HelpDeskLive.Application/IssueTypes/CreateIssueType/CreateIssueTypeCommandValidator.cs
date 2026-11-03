
using FluentValidation;

namespace HelpDeskLive.Application.IssueTypes.CreateIssueType;

public sealed class CreateIssueTypeCommandValidator
    : AbstractValidator<CreateIssueTypeCommand>
{
    public CreateIssueTypeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(150);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(1000);

        RuleFor(x => x.DepartmentId)
            .GreaterThan(0);

        RuleFor(x => x.DefaultPriority)
            .IsInEnum();
    }
}
