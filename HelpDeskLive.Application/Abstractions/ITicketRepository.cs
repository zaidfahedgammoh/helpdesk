
using HelpDeskLive.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace HelpDeskLive.Application.Abstractions;

public interface ITicketRepository
{
    Task AddAsync(Ticket ticket, CancellationToken cancellationToken);
    Task<bool> DepartmentExistsAsync(int departmentId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
