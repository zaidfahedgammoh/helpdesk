using MediatR;

namespace HelpDeskLive.Application.Tickets.CreateTicket;

public sealed record CreateTicketCommand(
    int IssueTypeId,
    string Description) : IRequest<int>;