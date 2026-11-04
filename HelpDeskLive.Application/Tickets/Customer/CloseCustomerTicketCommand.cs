using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Enums;
using MediatR;

namespace HelpDeskLive.Application.Tickets.Customer;

public sealed record CloseCustomerTicketCommand(int TicketId, int CustomerId) : IRequest;

public sealed class CloseCustomerTicketCommandHandler : IRequestHandler<CloseCustomerTicketCommand>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketNotificationService _notificationService;

    public CloseCustomerTicketCommandHandler(
        ITicketRepository ticketRepository,
        ITicketNotificationService notificationService)
    {
        _ticketRepository = ticketRepository;
        _notificationService = notificationService;
    }

    public async Task Handle(CloseCustomerTicketCommand request, CancellationToken ct)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);
        if (ticket is null || ticket.CustomerId != request.CustomerId)
            throw new KeyNotFoundException($"Ticket #{request.TicketId} was not found.");

        if (ticket.ApprovalStatus != TicketApprovalStatus.Approved ||
            ticket.Status is TicketStatus.Resolved or TicketStatus.Closed)
        {
            throw new InvalidOperationException("Only an approved, unresolved ticket can be closed by its customer.");
        }

        ticket.CloseByCustomer();
        await _ticketRepository.SaveChangesAsync(ct);
        await _notificationService.NotifyTicketClosedAsync(ticket, ct);
    }
}
