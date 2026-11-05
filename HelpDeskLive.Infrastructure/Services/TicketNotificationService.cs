using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace HelpDeskLive.Infrastructure.Services;

public sealed class TicketNotificationService : ITicketNotificationService
{
    private readonly ILogger<TicketNotificationService> _logger;

    public TicketNotificationService(ILogger<TicketNotificationService> logger)
    {
        _logger = logger;
    }

    public Task NotifyTicketClosedAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "Ticket closure notification delivered in-process. Ticket #{TicketId} ('{Title}') closed at {ClosedAt}. Customer #{CustomerId}, Agent #{AgentId}.",
            ticket.Id,
            ticket.Title,
            ticket.ClosedAt ?? DateTime.UtcNow,
            ticket.CustomerId,
            ticket.AssignedAgentId);

        return Task.CompletedTask;
    }
}
