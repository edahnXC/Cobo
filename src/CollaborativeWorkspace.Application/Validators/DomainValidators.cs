using CollaborativeWorkspace.Application.DTOs.Workspace;
using CollaborativeWorkspace.Application.DTOs.Channel;
using CollaborativeWorkspace.Application.DTOs.Message;
using CollaborativeWorkspace.Application.DTOs.Complaint;
using FluentValidation;

namespace CollaborativeWorkspace.Application.Validators;

public class CreateWorkspaceValidator : AbstractValidator<CreateWorkspaceRequest>
{
    public CreateWorkspaceValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Workspace name is required.")
            .MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class InviteMemberValidator : AbstractValidator<InviteMemberRequest>
{
    public InviteMemberValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class CreateChannelValidator : AbstractValidator<CreateChannelRequest>
{
    private static readonly string[] ValidTypes = ["text", "voice", "game"];

    public CreateChannelValidator()
    {
        RuleFor(x => x.ChannelName)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[a-z0-9\-_]+$").WithMessage("Channel name must be lowercase alphanumeric with hyphens/underscores only.");
        RuleFor(x => x.ChannelType)
            .Must(t => ValidTypes.Contains(t.ToLower()))
            .WithMessage("Channel type must be 'text', 'voice', or 'game'.");
    }
}

public class SendMessageValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content cannot be empty.")
            .MaximumLength(10000).WithMessage("Message cannot exceed 10,000 characters.");
    }
}

public class EditMessageValidator : AbstractValidator<EditMessageRequest>
{
    public EditMessageValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(10000);
    }
}

public class SubmitComplaintValidator : AbstractValidator<SubmitComplaintRequest>
{
    private static readonly string[] ValidCategories =
        ["Harassment", "Spam", "InappropriateContent", "TechnicalIssue", "Other"];

    public SubmitComplaintValidator()
    {
        RuleFor(x => x.Category)
            .Must(c => ValidCategories.Contains(c))
            .WithMessage("Invalid complaint category.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x)
            .Must(x => x.ReportedUserId.HasValue || x.WorkspaceId.HasValue || x.MessageId.HasValue)
            .WithMessage("At least one of ReportedUserId, WorkspaceId, or MessageId must be provided.");
    }
}
