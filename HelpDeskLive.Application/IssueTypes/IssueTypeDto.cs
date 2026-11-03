
using HelpDeskLive.Domain.Enums;

namespace HelpDeskLive.Application.IssueTypes.DTOs;

public sealed record IssueTypeDto(
    int Id,
    string Name,
    string Description,
    int DepartmentId,
    TicketPriority DefaultPriority
);
