using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Application.Chat;
using HelpDeskLive.Application.Common.Exceptions;
using HelpDeskLive.Domain.Enums;
using MediatR;

namespace HelpDeskLive.Application.Tickets.GetTicketMessages;

public sealed record GetTicketMessagesQuery(int TicketId) : IRequest<IReadOnlyList<ChatMessageDto>>;

public sealed class GetTicketMessagesQueryHandler : IRequestHandler<GetTicketMessagesQuery, IReadOnlyList<ChatMessageDto>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IChatRepository _chatRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;

    public GetTicketMessagesQueryHandler(
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

    public async Task<IReadOnlyList<ChatMessageDto>> Handle(GetTicketMessagesQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is not int userId)
            throw new UnauthorizedException();

        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);
        if (ticket is null)
            throw new NotFoundException("Ticket", request.TicketId);

        var isCustomer = _currentUser.Role == UserRole.Customer.ToString() && ticket.CustomerId == userId;
        var isAssignedAgent = _currentUser.Role == UserRole.Agent.ToString() && ticket.AssignedAgentId == userId;
        var isAdmin = _currentUser.Role == UserRole.Admin.ToString();

        if (!isCustomer && !isAssignedAgent && !isAdmin)
            throw new ForbiddenException("You are not authorized to view messages for this ticket.");

        var messages = await _chatRepository.GetMessagesByTicketIdAsync(request.TicketId, ct);
        var dtos = new List<ChatMessageDto>(messages.Count);

        foreach (var msg in messages)
        {
            var sender = await _userRepository.GetByIdAsync(msg.SenderId, ct);
            dtos.Add(new ChatMessageDto(
                msg.Id,
                msg.TicketId,
                msg.SenderId,
                sender?.Name ?? "User",
                msg.Content,
                msg.SentAt));
        }

        return dtos;
    }
}
