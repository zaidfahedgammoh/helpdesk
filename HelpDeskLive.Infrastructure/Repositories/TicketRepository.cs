
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using HelpDeskLive.Domain.Enums;
namespace HelpDeskLive.Infrastructure.Repositories;

public sealed class TicketRepository : ITicketRepository
{
    private readonly HelpDeskDbContext _dbContext;

    public TicketRepository(HelpDeskDbContext dbContext)
    {
        _dbContext = dbContext;
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
                user => user.Id == customerId
                    && user.Role == UserRole.Customer,
                cancellationToken);
    }

    public async Task<int> CountOpenTicketsForCustomerAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Tickets
            .CountAsync(
                ticket => ticket.CustomerId == customerId
                    && ticket.Status == TicketStatus.Open,
                cancellationToken);
    }
    public async Task AddAsync(
        Ticket ticket,
        CancellationToken cancellationToken)
    {
        await _dbContext.Tickets.AddAsync(ticket, cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
