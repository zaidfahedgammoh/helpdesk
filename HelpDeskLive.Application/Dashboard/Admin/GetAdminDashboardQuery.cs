using MediatR;

namespace HelpDeskLive.Application.Dashboard.Admin;

public sealed record GetAdminDashboardQuery : IRequest<AdminDashboardDto>;

public sealed record DepartmentTicketStatsDto(
    int DepartmentId,
    string DepartmentName,
    int TotalTickets,
    int OpenTickets,
    int InProgressTickets,
    int ResolvedTickets,
    int ClosedTickets,
    int EscalatedTickets
);

public sealed record AgentTicketStatsDto(
    int AgentId,
    string AgentName,
    string DepartmentName,
    int ActiveTickets,
    int ResolvedTickets,
    int ClosedTickets,
    int TotalHandled
);

public sealed record EscalatedTicketStatsDto(
    int TotalEscalations,
    int CurrentlyEscalatedTickets,
    int ReassignedEscalations,
    double AverageResolutionAfterEscalationHours
);

public sealed record ResolutionClosingTimeStatsDto(
    double AverageResolutionTimeHours,
    double AverageClosingTimeHours,
    int TotalResolvedSample,
    int TotalClosedSample
);

public sealed record AdminDashboardDto(
    int TotalTickets,
    int OpenTickets,
    int InProgressTickets,
    int ResolvedTickets,
    int ClosedTickets,
    int TotalAgents,
    int TotalCustomers,
    IReadOnlyList<DepartmentTicketStatsDto> DepartmentStats,
    IReadOnlyList<AgentTicketStatsDto> AgentStats,
    EscalatedTicketStatsDto EscalatedStats,
    ResolutionClosingTimeStatsDto PerformanceStats
);
