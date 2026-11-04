using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;

namespace HelpDeskLive.Application.Abstractions;

public interface ITicketRepository : IGenericRepository<Ticket>
{
    Task<Ticket?> GetByIdWithEscalationsAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> DepartmentExistsAsync(
        int departmentId,
        CancellationToken cancellationToken = default);

    Task<bool> CustomerExistsAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<int> CountOpenTicketsForCustomerAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Ticket>> GetByCustomerIdAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Ticket>> GetDepartmentQueueTicketsAsync(
        int departmentId,
        int agentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Ticket>> GetPendingApprovalTicketsAsync(
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveLiveChatAsync(
        int agentId,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveTicketsForAgentAsync(
        int agentId,
        CancellationToken cancellationToken = default);

    Task<bool> HasWaitingLiveTicketsInDepartmentAsync(
        int departmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketEscalation>> GetEscalationsByTicketIdAsync(
        int ticketId,
        CancellationToken cancellationToken = default);

    Task AddEscalationAsync(
        TicketEscalation escalation,
        CancellationToken cancellationToken = default);

    Task<int> CountAllAsync(
        CancellationToken cancellationToken = default);

    Task<int> CountByStatusAsync(
        TicketStatus status,
        CancellationToken cancellationToken = default);

    Task<int> CountByPriorityAsync(
        TicketPriority priority,
        CancellationToken cancellationToken = default);

    Task<int> CountAssignedToAgentAsync(
        int agentId,
        CancellationToken cancellationToken = default);

    Task<int> CountAssignedToAgentByStatusAsync(
        int agentId,
        TicketStatus status,
        CancellationToken cancellationToken = default);

    Task<int> CountAssignedToAgentByPriorityAsync(
        int agentId,
        TicketPriority priority,
        CancellationToken cancellationToken = default);

    Task AssignToAvailableAgentAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default);
}
