using FluentValidation;
using HelpDeskLive.Application.Chat;
using MediatR;

namespace HelpDeskLive.Application.Tickets.SendMessage;

public sealed record SendMessageCommand(int TicketId, int SenderId, string Content) : IRequest<ChatMessageDto>;

public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.TicketId).GreaterThan(0);
        RuleFor(x => x.SenderId).GreaterThan(0);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(5000);
    }
}

public sealed class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, ChatMessageDto>
{
    private readonly ISender _sender;

    public SendMessageCommandHandler(ISender sender)
    {
        _sender = sender;
    }

    public Task<ChatMessageDto> Handle(SendMessageCommand request, CancellationToken ct)
    {
        return _sender.Send(new SendChatMessageCommand(request.TicketId, request.SenderId, request.Content), ct);
    }
}
