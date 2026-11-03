using HelpDeskLive.Application.Abstractions;
using MediatR;

namespace HelpDeskLive.Application.Tickets.GetMyTickets;

public sealed class GetMyTicketsQueryHandler
    : IRequestHandler<GetMyTicketsQuery, IReadOnlyList<TicketListItemDto>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ICurrentUser _currentUser;

    public GetMyTicketsQueryHandler(
        ITicketRepository ticketRepository,
        ICurrentUser currentUser)
    {
        _ticketRepository = ticketRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TicketListItemDto>> Handle(
        GetMyTicketsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not int customerId ||
            _currentUser.Role != "Customer")
        {
            throw new UnauthorizedAccessException(
                "Only authenticated customers can view their tickets.");
        }

        var tickets = await _ticketRepository.GetByCustomerIdAsync(
            customerId,
            cancellationToken);

        return tickets
            .Select(ticket => new TicketListItemDto(
                ticket.Id,
                ticket.Title,
                ticket.Status,
                ticket.Priority,
                ticket.CreatedAt))
            .ToList();
    }
}