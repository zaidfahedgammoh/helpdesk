using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Application.Common.Exceptions;
using HelpDeskLive.Domain.Enums;
using MediatR;

namespace HelpDeskLive.Application.Tickets.GetMyTickets;

public sealed class GetMyTicketsQueryHandler : IRequestHandler<GetMyTicketsQuery, IReadOnlyList<TicketListItemDto>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ICurrentUser _currentUser;

    public GetMyTicketsQueryHandler(ITicketRepository ticketRepository, ICurrentUser currentUser)
    {
        _ticketRepository = ticketRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TicketListItemDto>> Handle(GetMyTicketsQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is not int customerId || _currentUser.Role != UserRole.Customer.ToString())
            throw new ForbiddenException("Only authenticated customers can view their tickets.");

        var tickets = await _ticketRepository.GetByCustomerIdAsync(customerId, ct);

        IEnumerable<Domain.Entities.Ticket> query = tickets;

        if (request.Status.HasValue)
        {
            query = query.Where(t => t.Status == request.Status.Value);
        }

        if (request.PageNumber.HasValue && request.PageSize.HasValue && request.PageNumber > 0 && request.PageSize > 0)
        {
            query = query.Skip((request.PageNumber.Value - 1) * request.PageSize.Value).Take(request.PageSize.Value);
        }

        return query.Select(t => new TicketListItemDto(t.Id, t.Title, t.Status, t.Priority, t.ApprovalStatus, t.CreatedAt, t.AssignedAgentId)).ToList();
    }
}
