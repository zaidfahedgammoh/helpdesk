using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;
using HelpDeskLive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskLive.Infrastructure.Repositories;

public sealed class TicketRepository : GenericRepository<Ticket>, ITicketRepository
{
    private readonly HelpDeskDbContext _dbContext;

    public TicketRepository(HelpDeskDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public override Task<Ticket?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Tickets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public Task<Ticket?> GetByIdWithEscalationsAsync(int id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Tickets
            .Include(t => t.Escalations)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
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

    public override async Task AddAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Tickets.AddAsync(ticket, cancellationToken);
    }

    public async Task<bool> DepartmentExistsAsync(
        int departmentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Departments
            .AnyAsync(
                department => department.Id == departmentId,
                cancellationToken);
    }

    public async Task<bool> CustomerExistsAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AnyAsync(
                user => user.Id == customerId &&
                        user.Role == UserRole.Customer,
                cancellationToken);
    }

    public async Task<int> CountOpenTicketsForCustomerAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tickets
            .CountAsync(
                ticket => ticket.CustomerId == customerId &&
                          ticket.Status == TicketStatus.Open &&
                          ticket.ApprovalStatus != TicketApprovalStatus.Rejected,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Ticket>> GetByCustomerIdAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.CustomerId == customerId)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Ticket>> GetDepartmentQueueTicketsAsync(
        int departmentId,
        int agentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tickets
            .AsNoTracking()
            .Where(t => t.DepartmentId == departmentId &&
                t.ApprovalStatus == TicketApprovalStatus.Approved &&
                (t.Status == TicketStatus.Open || t.Status == TicketStatus.Escalated ||
                 (t.Status == TicketStatus.InProgress && t.AssignedAgentId == agentId)))
            .OrderByDescending(t => t.Status == TicketStatus.Escalated)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Ticket>> GetPendingApprovalTicketsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tickets
            .AsNoTracking()
            .Where(t => t.ApprovalStatus == TicketApprovalStatus.Pending)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasActiveLiveChatAsync(
        int agentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Tickets.AnyAsync(t =>
            t.AssignedAgentId == agentId &&
            t.Status == TicketStatus.InProgress &&
            t.ApprovalStatus == TicketApprovalStatus.Approved &&
            (t.Priority == TicketPriority.High || t.Priority == TicketPriority.Critical),
            cancellationToken);
    }

    public Task<int> CountActiveTicketsForAgentAsync(
        int agentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Tickets.CountAsync(t =>
            t.AssignedAgentId == agentId &&
            t.Status == TicketStatus.InProgress &&
            t.ApprovalStatus == TicketApprovalStatus.Approved,
            cancellationToken);
    }

    public Task<bool> HasWaitingLiveTicketsInDepartmentAsync(
        int departmentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Tickets.AnyAsync(t =>
            t.DepartmentId == departmentId &&
            t.AssignedAgentId == null &&
            (t.Status == TicketStatus.Open || t.Status == TicketStatus.Escalated) &&
            t.ApprovalStatus == TicketApprovalStatus.Approved &&
            (t.Priority == TicketPriority.High || t.Priority == TicketPriority.Critical),
            cancellationToken);
    }

    public async Task<IReadOnlyList<TicketEscalation>> GetEscalationsByTicketIdAsync(
        int ticketId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TicketEscalations
            .AsNoTracking()
            .Where(e => e.TicketId == ticketId)
            .OrderByDescending(e => e.EscalatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddEscalationAsync(
        TicketEscalation escalation,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.TicketEscalations.AddAsync(escalation, cancellationToken);
    }

    public async Task<int> CountAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tickets
            .CountAsync(cancellationToken);
    }

    public async Task<int> CountByStatusAsync(
        TicketStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tickets
            .CountAsync(
                ticket => ticket.Status == status && ticket.ApprovalStatus == TicketApprovalStatus.Approved,
                cancellationToken);
    }

    public async Task<int> CountAssignedToAgentAsync(
        int agentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tickets
            .CountAsync(
                ticket => ticket.AssignedAgentId == agentId,
                cancellationToken);
    }

    public async Task<int> CountAssignedToAgentByStatusAsync(
        int agentId,
        TicketStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tickets
            .CountAsync(
                ticket => ticket.AssignedAgentId == agentId &&
                          ticket.Status == status && ticket.ApprovalStatus == TicketApprovalStatus.Approved,
                cancellationToken);
    }

    public async Task AssignToAvailableAgentAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        if (ticket.DepartmentId is not int departmentId || ticket.ApprovalStatus != TicketApprovalStatus.Approved) return;
        var agent = await _dbContext.Users
            .Where(user => user.Role == UserRole.Agent && user.DepartmentId == departmentId)
            .Select(user => new { User = user, Load = _dbContext.Tickets.Count(t => t.AssignedAgentId == user.Id && t.Status == TicketStatus.InProgress && t.ApprovalStatus == TicketApprovalStatus.Approved) })
            .Where(item => item.Load < 5)
            .OrderBy(item => item.Load)
            .ThenBy(item => item.User.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (agent is not null) ticket.AssignToAgent(agent.User.Id);
    }

    public override async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
