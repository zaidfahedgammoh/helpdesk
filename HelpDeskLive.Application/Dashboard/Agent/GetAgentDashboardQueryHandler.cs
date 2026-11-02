
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Enums;
using MediatR;

namespace HelpDeskLive.Application.Dashboard.Agent;

public sealed class GetAgentDashboardQueryHandler
    : IRequestHandler<GetAgentDashboardQuery, AgentDashboardDto>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ICurrentUser _currentUser;

    public GetAgentDashboardQueryHandler(
        ITicketRepository ticketRepository,
        ICurrentUser currentUser)
    {
        _ticketRepository = ticketRepository;
        _currentUser = currentUser;
    }

    public async Task<AgentDashboardDto> Handle(
        GetAgentDashboardQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not int agentId ||
            _currentUser.Role != "Agent")
        {
            throw new UnauthorizedAccessException(
                "Only authenticated agents can access this dashboard.");
        }

        var assignedTickets =
            await _ticketRepository.CountAssignedToAgentAsync(
                agentId,
                cancellationToken);

        var openTickets =
            await _ticketRepository.CountAssignedToAgentByStatusAsync(
                agentId,
                TicketStatus.Open,
                cancellationToken);

        var inProgressTickets =
            await _ticketRepository.CountAssignedToAgentByStatusAsync(
                agentId,
                TicketStatus.InProgress,
                cancellationToken);

        var resolvedTickets =
            await _ticketRepository.CountAssignedToAgentByStatusAsync(
                agentId,
                TicketStatus.Resolved,
                cancellationToken);

        var closedTickets =
            await _ticketRepository.CountAssignedToAgentByStatusAsync(
                agentId,
                TicketStatus.Closed,
                cancellationToken);

        return new AgentDashboardDto(
            assignedTickets,
            openTickets,
            inProgressTickets,
            resolvedTickets,
            closedTickets);
    }
}
