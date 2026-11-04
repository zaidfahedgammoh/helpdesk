using FluentValidation;
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Application.Common.Exceptions;
using HelpDeskLive.Domain.Enums;
using MediatR;

namespace HelpDeskLive.Application.Tickets.AssignTicket;

public sealed record AssignTicketCommand(int TicketId, int AgentId) : IRequest;

public sealed class AssignTicketCommandValidator : AbstractValidator<AssignTicketCommand>
{
    public AssignTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).GreaterThan(0);
        RuleFor(x => x.AgentId).GreaterThan(0);
    }
}

public sealed class AssignTicketCommandHandler : IRequestHandler<AssignTicketCommand>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;

    public AssignTicketCommandHandler(ITicketRepository ticketRepository, IUserRepository userRepository)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
    }

    public async Task Handle(AssignTicketCommand request, CancellationToken ct)
    {
        var agent = await _userRepository.GetByIdAsync(request.AgentId, ct);
        if (agent is null || agent.Role != UserRole.Agent || agent.DepartmentId is not int departmentId)
            throw new NotFoundException("Agent", request.AgentId);

        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);
        if (ticket is null)
            throw new NotFoundException("Ticket", request.TicketId);

        if (ticket.DepartmentId != departmentId)
            throw new ConflictException("Ticket does not belong to the agent's department.");

        var activeCount = await _ticketRepository.CountActiveTicketsForAgentAsync(request.AgentId, ct);
        if (activeCount >= 5)
            throw new ConflictException("Agent has reached the maximum limit of 5 in-progress tickets.");

        ticket.AssignToAgent(request.AgentId);
        await _ticketRepository.SaveChangesAsync(ct);
    }
}
