
using HelpDeskLive.Domain.Enums;
using MediatR;

namespace HelpDeskLive.Application.Tickets.CreateTicket;

public sealed record CreateTicketCommand(
    string Title,
    string Description,
    TicketPriority Priority,
    int DepartmentId) : IRequest<int>;
