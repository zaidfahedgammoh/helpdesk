using MediatR;

namespace HelpDeskLive.Application.Tickets.GetMyTickets;

public sealed record GetMyTicketsQuery
    : IRequest<IReadOnlyList<TicketListItemDto>>;