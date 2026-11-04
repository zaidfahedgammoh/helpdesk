using HelpDeskLive.Application.Abstractions;
using MediatR;

namespace HelpDeskLive.Application.Tickets.GetMyTickets;

public sealed class GetMyTicketsQueryHandler : IRequestHandler<GetMyTicketsQuery, IReadOnlyList<TicketListItemDto>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ICurrentUser _currentUser;
    public GetMyTicketsQueryHandler(ITicketRepository ticketRepository, ICurrentUser currentUser)
    { _ticketRepository = ticketRepository; _currentUser = currentUser; }

    public async Task<IReadOnlyList<TicketListItemDto>> Handle(GetMyTicketsQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is not int customerId || _currentUser.Role != "Customer")
            throw new UnauthorizedAccessException("Only authenticated customers can view their tickets.");
        var tickets = await _ticketRepository.GetByCustomerIdAsync(customerId, ct);
        return tickets.Select(t => new TicketListItemDto(t.Id, t.Title, t.Status, t.Priority, t.ApprovalStatus, t.CreatedAt, t.AssignedAgentId)).ToList();
    }
}
