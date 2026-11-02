
using MediatR;

namespace HelpDeskLive.Application.Dashboard.Agent;

public sealed record GetAgentDashboardQuery
    : IRequest<AgentDashboardDto>;

public sealed record AgentDashboardDto(
    int AssignedTickets,
    int OpenTickets,
    int InProgressTickets,
    int ResolvedTickets,
    int ClosedTickets
);
