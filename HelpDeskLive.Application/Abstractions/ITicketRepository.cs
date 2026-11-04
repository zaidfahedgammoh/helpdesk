
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;

namespace HelpDeskLive.Application.Abstractions;

public interface ITicketRepository
{
    Task<int?> GetDepartmentIdByTicketIdAsync(int ticketId, CancellationToken cancellationToken);

    Task AddAsync(
        Ticket ticket,
        CancellationToken cancellationToken);

    Task<bool> DepartmentExistsAsync(
        int departmentId,
        CancellationToken cancellationToken);

    Task<bool> CustomerExistsAsync(
        int customerId,
        CancellationToken cancellationToken);

    Task<int> CountOpenTicketsForCustomerAsync(
        int customerId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Ticket>> GetByCustomerIdAsync(
        int customerId,
        CancellationToken cancellationToken);

    Task<int> CountAllAsync(
        CancellationToken cancellationToken);

    Task<int> CountByStatusAsync(
        TicketStatus status,
        CancellationToken cancellationToken);

    Task<int> CountByPriorityAsync(
        TicketPriority priority,
        CancellationToken cancellationToken);

    Task<int> CountAssignedToAgentAsync(
        int agentId,
        CancellationToken cancellationToken);

    Task<int> CountAssignedToAgentByStatusAsync(
        int agentId,
        TicketStatus status,
        CancellationToken cancellationToken);

    Task<int> CountAssignedToAgentByPriorityAsync(
        int agentId,
        TicketPriority priority,
        CancellationToken cancellationToken);

    Task AssignToAvailableAgentAsync(Ticket ticket, CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}
