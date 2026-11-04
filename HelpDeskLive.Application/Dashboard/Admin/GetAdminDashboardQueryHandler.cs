using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Enums;
using MediatR;

namespace HelpDeskLive.Application.Dashboard.Admin;

public sealed class GetAdminDashboardQueryHandler
    : IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAdminReportingService _reportingService;

    public GetAdminDashboardQueryHandler(
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        IAdminReportingService reportingService)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _reportingService = reportingService;
    }

    public async Task<AdminDashboardDto> Handle(
        GetAdminDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var totalTickets = await _ticketRepository.CountAllAsync(cancellationToken);
        var openTickets = await _ticketRepository.CountByStatusAsync(TicketStatus.Open, cancellationToken);
        var inProgressTickets = await _ticketRepository.CountByStatusAsync(TicketStatus.InProgress, cancellationToken);
        var resolvedTickets = await _ticketRepository.CountByStatusAsync(TicketStatus.Resolved, cancellationToken);
        var closedTickets = await _ticketRepository.CountByStatusAsync(TicketStatus.Closed, cancellationToken);

        var totalAgents = await _userRepository.CountByRoleAsync(UserRole.Agent, cancellationToken);
        var totalCustomers = await _userRepository.CountByRoleAsync(UserRole.Customer, cancellationToken);

        var departmentStats = await _reportingService.GetDepartmentTicketStatsViaLinqAsync(cancellationToken);
        var agentStats = await _reportingService.GetAgentTicketStatsViaDapperAsync(cancellationToken);
        var escalatedStats = await _reportingService.GetEscalatedTicketStatsViaStoredProcedureAsync(cancellationToken);
        var performanceStats = await _reportingService.GetPerformanceStatsViaAdoNetAsync(cancellationToken);

        return new AdminDashboardDto(
            totalTickets,
            openTickets,
            inProgressTickets,
            resolvedTickets,
            closedTickets,
            totalAgents,
            totalCustomers,
            departmentStats,
            agentStats,
            escalatedStats,
            performanceStats);
    }
}
