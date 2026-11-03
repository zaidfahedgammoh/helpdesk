using HelpDeskLive.Domain.Enums;

namespace HelpDeskLive.Application.Tickets.GetMyTickets;

public sealed record TicketListItemDto(int Id, string Title, TicketStatus Status, TicketPriority? Priority, TicketApprovalStatus ApprovalStatus, DateTime CreatedAt);
