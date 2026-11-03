
using HelpDeskLive.Domain.Enums;
using MediatR;

namespace HelpDeskLive.Application.IssueTypes.CreateIssueType;

public sealed record CreateIssueTypeCommand(
    string Name,
    string Description,
    int DepartmentId,
    TicketPriority DefaultPriority
) : IRequest<int>;
