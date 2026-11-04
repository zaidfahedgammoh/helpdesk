using HelpDeskLive.Domain.Entities;

namespace HelpDeskLive.Application.Abstractions;

public interface ITicketNotificationService
{
    Task NotifyTicketClosedAsync(Ticket ticket, CancellationToken cancellationToken = default);
}
