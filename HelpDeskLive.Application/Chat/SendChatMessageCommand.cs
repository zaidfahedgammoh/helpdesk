using FluentValidation;
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;
using MediatR;

namespace HelpDeskLive.Application.Chat;

public sealed record SendChatMessageCommand(
    int TicketId,
    int SenderId,
    string Content
) : IRequest<ChatMessageDto>;

public sealed class SendChatMessageCommandValidator : AbstractValidator<SendChatMessageCommand>
{
    public SendChatMessageCommandValidator()
    {
        RuleFor(x => x.TicketId).GreaterThan(0).WithMessage("Valid ticket id is required.");
        RuleFor(x => x.SenderId).GreaterThan(0).WithMessage("Valid sender id is required.");
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content cannot be empty.")
            .MaximumLength(5000).WithMessage("Message cannot exceed 5000 characters.");
    }
}

public sealed class SendChatMessageCommandHandler : IRequestHandler<SendChatMessageCommand, ChatMessageDto>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IChatRepository _chatRepository;
    private readonly IUserRepository _userRepository;

    public SendChatMessageCommandHandler(
        ITicketRepository ticketRepository,
        IChatRepository chatRepository,
        IUserRepository userRepository)
    {
        _ticketRepository = ticketRepository;
        _chatRepository = chatRepository;
        _userRepository = userRepository;
    }

    public async Task<ChatMessageDto> Handle(SendChatMessageCommand request, CancellationToken ct)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);
        if (ticket is null)
            throw new KeyNotFoundException($"Ticket #{request.TicketId} was not found.");

        var sender = await _userRepository.GetByIdAsync(request.SenderId, ct);
        if (sender is null)
            throw new UnauthorizedAccessException("Sender account could not be found.");

        var isCustomer = sender.Role == UserRole.Customer && ticket.CustomerId == request.SenderId;
        var isAgent = sender.Role == UserRole.Agent && ticket.AssignedAgentId == request.SenderId;

        if (!isCustomer && !isAgent)
            throw new UnauthorizedAccessException("You are not allowed to send messages in this chat.");

        if (ticket.ApprovalStatus != TicketApprovalStatus.Approved ||
            ticket.Priority is not (TicketPriority.High or TicketPriority.Critical) ||
            ticket.Status is not (TicketStatus.Open or TicketStatus.InProgress))
        {
            throw new InvalidOperationException("Live chat is only active for approved High and Critical tickets that are waiting or in progress.");
        }

        var message = new ChatMessage(request.TicketId, request.SenderId, request.Content.Trim());
        await _chatRepository.AddMessageAsync(message, ct);
        await _chatRepository.SaveChangesAsync(ct);

        return new ChatMessageDto(
            message.Id,
            message.TicketId,
            message.SenderId,
            sender.Name,
            message.Content,
            message.SentAt);
    }
}
