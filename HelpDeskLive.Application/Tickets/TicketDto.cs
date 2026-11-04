using HelpDeskLive.Domain.Enums;

namespace HelpDeskLive.Application.Tickets;

public sealed record TicketDto(
    int Id,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority? Priority,
    TicketApprovalStatus ApprovalStatus,
    int? DepartmentId,
    int CustomerId,
    int? AssignedAgentId,
    string? AgentName,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt
);
