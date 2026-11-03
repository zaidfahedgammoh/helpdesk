
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;
using HelpDeskLive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskLive.Infrastructure.Repositories;

public sealed class TicketRepository : ITicketRepository
{
    private readonly HelpDeskDbContext _dbContext;

    public TicketRepository(HelpDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<int> CountByPriorityAsync(
    TicketPriority priority,
    CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tickets
            .CountAsync(t => t.Priority == priority, cancellationToken);
    }

    public async Task<int> CountAssignedToAgentByPriorityAsync(
        int agentId,
        TicketPriority priority,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tickets
            .CountAsync(
                t => t.AssignedAgentId == agentId && t.Priority == priority,
                cancellationToken);
    }
    public async Task AddAsync(
        Ticket ticket,
        CancellationToken cancellationToken)
    {
        await _dbContext.Tickets.AddAsync(ticket, cancellationToken);
    }

    public async Task<bool> DepartmentExistsAsync(
        int departmentId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Departments
            .AnyAsync(
                department => department.Id == departmentId,
                cancellationToken);
    }

    public async Task<bool> CustomerExistsAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AnyAsync(
                user => user.Id == customerId &&
                        user.Role == UserRole.Customer,
                cancellationToken);
    }

    public async Task<int> CountOpenTicketsForCustomerAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Tickets
            .CountAsync(
                ticket => ticket.CustomerId == customerId &&
                          ticket.Status == TicketStatus.Open,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Ticket>> GetByCustomerIdAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.CustomerId == customerId)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAllAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.Tickets
            .CountAsync(cancellationToken);
    }

    public async Task<int> CountByStatusAsync(
        TicketStatus status,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Tickets
            .CountAsync(
                ticket => ticket.Status == status,
                cancellationToken);
    }

    public async Task<int> CountAssignedToAgentAsync(
        int agentId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Tickets
            .CountAsync(
                ticket => ticket.AssignedAgentId == agentId,
                cancellationToken);
    }

    public async Task<int> CountAssignedToAgentByStatusAsync(
        int agentId,
        TicketStatus status,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Tickets
            .CountAsync(
                ticket => ticket.AssignedAgentId == agentId &&
                          ticket.Status == status,
                cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
