using FluentValidation;
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Application.Common.Exceptions;
using HelpDeskLive.Domain.Enums;
using MediatR;

namespace HelpDeskLive.Application.Tickets.CloseTicket;

public sealed record CloseTicketCommand(int TicketId) : IRequest;

public sealed class CloseTicketCommandValidator : AbstractValidator<CloseTicketCommand>
{
    public CloseTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).GreaterThan(0);
    }
}

public sealed class CloseTicketCommandHandler : IRequestHandler<CloseTicketCommand>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ICurrentUser _currentUser;
    private readonly ITicketNotificationService _notificationService;

    public CloseTicketCommandHandler(
        ITicketRepository ticketRepository,
        ICurrentUser currentUser,
        ITicketNotificationService notificationService)
    {
        _ticketRepository = ticketRepository;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task Handle(CloseTicketCommand request, CancellationToken ct)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);
        if (ticket is null)
            throw new NotFoundException("Ticket", request.TicketId);

        if (_currentUser.UserId is not int userId)
            throw new UnauthorizedException();

        if (_currentUser.Role == UserRole.Customer.ToString())
        {
            if (ticket.CustomerId != userId)
                throw new ForbiddenException("You cannot close a ticket that does not belong to you.");
        }
        else if (_currentUser.Role == UserRole.Agent.ToString())
        {
            if (ticket.AssignedAgentId != userId)
                throw new ForbiddenException("You cannot close a ticket that is not assigned to you.");
            if (ticket.Status != TicketStatus.Resolved)
                throw new ConflictException("Ticket must be marked as Resolved before it can be closed by an agent.");
        }

        ticket.ChangeStatus(TicketStatus.Closed);
        await _ticketRepository.SaveChangesAsync(ct);

        await _notificationService.NotifyTicketClosedAsync(ticket, ct);
    }
}
