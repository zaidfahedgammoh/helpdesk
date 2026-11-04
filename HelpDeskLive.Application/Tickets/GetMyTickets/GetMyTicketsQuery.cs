using HelpDeskLive.Domain.Enums;
using MediatR;

namespace HelpDeskLive.Application.Tickets.GetMyTickets;

public sealed record GetMyTicketsQuery(
    int? PageNumber = null,
    int? PageSize = null,
    TicketStatus? Status = null) : IRequest<IReadOnlyList<TicketListItemDto>>;
