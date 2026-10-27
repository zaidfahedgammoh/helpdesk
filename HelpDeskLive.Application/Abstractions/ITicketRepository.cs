
using HelpDeskLive.Domain.Entities;

namespace HelpDeskLive.Application.Abstractions;

public interface ITicketRepository
{
    Task AddAsync(Ticket ticket, CancellationToken cancellationToken);

    Task<bool> DepartmentExistsAsync(
        int departmentId,
        CancellationToken cancellationToken);

    Task<bool> CustomerExistsAsync(
        int customerId,
        CancellationToken cancellationToken);

    Task<int> CountOpenTicketsForCustomerAsync(
        int customerId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
