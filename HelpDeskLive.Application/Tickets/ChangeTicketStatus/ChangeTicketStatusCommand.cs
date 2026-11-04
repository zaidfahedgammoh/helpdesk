using FluentValidation;
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Application.Common.Exceptions;
using HelpDeskLive.Domain.Enums;
using MediatR;

namespace HelpDeskLive.Application.Tickets.ChangeTicketStatus;

public sealed record ChangeTicketStatusCommand(int TicketId, TicketStatus NewStatus) : IRequest;

public sealed class ChangeTicketStatusCommandValidator : AbstractValidator<ChangeTicketStatusCommand>
{
    public ChangeTicketStatusCommandValidator()
    {
        RuleFor(x => x.TicketId).GreaterThan(0);
        RuleFor(x => x.NewStatus).IsInEnum();
    }
}

public sealed class ChangeTicketStatusCommandHandler : IRequestHandler<ChangeTicketStatusCommand>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ICurrentUser _currentUser;

    public ChangeTicketStatusCommandHandler(ITicketRepository ticketRepository, ICurrentUser currentUser)
    {
        _ticketRepository = ticketRepository;
        _currentUser = currentUser;
    }

    public async Task Handle(ChangeTicketStatusCommand request, CancellationToken ct)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);
        if (ticket is null)
            throw new NotFoundException("Ticket", request.TicketId);

        if (_currentUser.UserId is not int userId)
            throw new UnauthorizedException();

        if (_currentUser.Role == UserRole.Agent.ToString() && ticket.AssignedAgentId != userId)
            throw new ForbiddenException("Only the assigned agent can change the ticket status.");

        ticket.ChangeStatus(request.NewStatus);
        await _ticketRepository.SaveChangesAsync(ct);
    }
}
