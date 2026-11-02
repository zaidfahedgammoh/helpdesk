
using MediatR;

namespace HelpDeskLive.Application.Dashboard.Admin;

public sealed record GetAdminDashboardQuery
    : IRequest<AdminDashboardDto>;

public sealed record AdminDashboardDto(
    int TotalTickets,
    int OpenTickets,
    int InProgressTickets,
    int ResolvedTickets,
    int ClosedTickets,
    int TotalAgents,
    int TotalCustomers
);
