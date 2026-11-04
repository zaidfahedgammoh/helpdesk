using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Enums;
using MediatR;

namespace HelpDeskLive.Application.Chat;

public sealed record GetChatMessagesQuery(int TicketId) : IRequest<ChatMessagesResultDto>;

public sealed class GetChatMessagesQueryHandler : IRequestHandler<GetChatMessagesQuery, ChatMessagesResultDto>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IChatRepository _chatRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;

    public GetChatMessagesQueryHandler(
        ITicketRepository ticketRepository,
        IChatRepository chatRepository,
        IUserRepository userRepository,
        ICurrentUser currentUser)
    {
        _ticketRepository = ticketRepository;
        _chatRepository = chatRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<ChatMessagesResultDto> Handle(GetChatMessagesQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is not int userId)
            throw new UnauthorizedAccessException("Authentication is required to view chat messages.");

        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);
        if (ticket is null)
            throw new KeyNotFoundException($"Ticket #{request.TicketId} was not found.");

        var isCustomer = _currentUser.Role == UserRole.Customer.ToString() && ticket.CustomerId == userId;
        var isAssignedAgent = _currentUser.Role == UserRole.Agent.ToString() && (ticket.AssignedAgentId == userId || ticket.AssignedAgentId == null);
        var isAdmin = _currentUser.Role == UserRole.Admin.ToString();

        if (!isCustomer && !isAssignedAgent && !isAdmin)
            throw new UnauthorizedAccessException("You are not authorized to view messages for this ticket.");

        var messages = await _chatRepository.GetMessagesByTicketIdAsync(request.TicketId, ct);
        var dtos = new List<ChatMessageDto>(messages.Count);

        foreach (var msg in messages)
        {
            var sender = await _userRepository.GetByIdAsync(msg.SenderId, ct);
            dtos.Add(new ChatMessageDto(
                msg.Id,
                msg.TicketId,
                msg.SenderId,
                sender?.Name ?? "Unknown",
                msg.Content,
                msg.SentAt));
        }

        return new ChatMessagesResultDto(
            ticket.Id,
            ticket.Status.ToString(),
            ticket.AssignedAgentId,
            ticket.AssignedAgentId is null,
            dtos);
    }
}
