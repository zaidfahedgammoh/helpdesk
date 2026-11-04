using FluentValidation;
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;
using MediatR;

namespace HelpDeskLive.Application.Tickets.Agent;

public sealed record AgentTicketDto(
    int Id,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority? Priority,
    int? DepartmentId,
    DateTime CreatedAt,
    int? AssignedAgentId
);

public sealed record GetAgentTicketsQuery(int AgentId) : IRequest<IReadOnlyList<AgentTicketDto>>;

public sealed class GetAgentTicketsQueryHandler : IRequestHandler<GetAgentTicketsQuery, IReadOnlyList<AgentTicketDto>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;

    public GetAgentTicketsQueryHandler(ITicketRepository ticketRepository, IUserRepository userRepository)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<AgentTicketDto>> Handle(GetAgentTicketsQuery request, CancellationToken ct)
    {
        var agent = await _userRepository.GetByIdAsync(request.AgentId, ct);
        if (agent is null || agent.Role != UserRole.Agent || agent.DepartmentId is not int departmentId)
            throw new UnauthorizedAccessException("Agent not found or has no assigned department.");

        var tickets = await _ticketRepository.GetDepartmentQueueTicketsAsync(departmentId, request.AgentId, ct);
        return tickets.Select(t => new AgentTicketDto(
            t.Id,
            t.Title,
            t.Description,
            t.Status,
            t.Priority,
            t.DepartmentId,
            t.CreatedAt,
            t.AssignedAgentId)).ToList();
    }
}

public sealed record StartTicketResult(int TicketId, TicketPriority? Priority, bool IsLiveChat, int DepartmentId);

public sealed record StartTicketCommand(int TicketId, int AgentId) : IRequest<StartTicketResult>;

public sealed class StartTicketCommandHandler : IRequestHandler<StartTicketCommand, StartTicketResult>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;

    public StartTicketCommandHandler(ITicketRepository ticketRepository, IUserRepository userRepository)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
    }

    public async Task<StartTicketResult> Handle(StartTicketCommand request, CancellationToken ct)
    {
        var agent = await _userRepository.GetByIdAsync(request.AgentId, ct);
        if (agent is null || agent.Role != UserRole.Agent || agent.DepartmentId is not int departmentId)
            throw new UnauthorizedAccessException("Agent does not exist or has no assigned department.");

        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);
        if (ticket is null ||
            ticket.DepartmentId != departmentId ||
            ticket.ApprovalStatus != TicketApprovalStatus.Approved ||
            (ticket.Status != TicketStatus.Open && ticket.Status != TicketStatus.Escalated) ||
            (ticket.AssignedAgentId != null && ticket.AssignedAgentId != request.AgentId))
        {
            throw new KeyNotFoundException("This ticket is no longer waiting in your department.");
        }

        var isLivePriority = ticket.Priority is TicketPriority.High or TicketPriority.Critical;

        var hasActiveLiveChat = await _ticketRepository.HasActiveLiveChatAsync(request.AgentId, ct);
        if (hasActiveLiveChat)
            throw new InvalidOperationException("Finish your current live chat before starting another ticket.");

        var activeCount = await _ticketRepository.CountActiveTicketsForAgentAsync(request.AgentId, ct);
        if (activeCount >= 5)
            throw new InvalidOperationException("You have reached the five-ticket workload limit.");

        if (!isLivePriority)
        {
            var liveWaiting = await _ticketRepository.HasWaitingLiveTicketsInDepartmentAsync(departmentId, ct);
            if (liveWaiting)
                throw new InvalidOperationException("A high-priority customer is waiting for live support.");
        }

        ticket.AssignToAgent(request.AgentId);
        ticket.ChangeStatus(TicketStatus.InProgress);
        await _ticketRepository.SaveChangesAsync(ct);

        return new StartTicketResult(ticket.Id, ticket.Priority, isLivePriority, departmentId);
    }
}

public sealed record ResolveTicketCommand(int TicketId, int AgentId) : IRequest;

public sealed class ResolveTicketCommandHandler : IRequestHandler<ResolveTicketCommand>
{
    private readonly ITicketRepository _ticketRepository;

    public ResolveTicketCommandHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task Handle(ResolveTicketCommand request, CancellationToken ct)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);
        if (ticket is null || ticket.AssignedAgentId != request.AgentId)
            throw new KeyNotFoundException($"Ticket #{request.TicketId} was not found or not assigned to you.");

        ticket.ChangeStatus(TicketStatus.Resolved);
        await _ticketRepository.SaveChangesAsync(ct);
    }
}

public sealed record EscalateTicketResult(int TicketId, int? DepartmentId, string Reason, DateTime EscalatedAt);

public sealed record EscalateTicketCommand(int TicketId, int AgentId, string Reason) : IRequest<EscalateTicketResult>;

public sealed class EscalateTicketCommandValidator : AbstractValidator<EscalateTicketCommand>
{
    public EscalateTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).GreaterThan(0);
        RuleFor(x => x.AgentId).GreaterThan(0);
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Escalation reason is required.")
            .MaximumLength(1000).WithMessage("Escalation reason cannot exceed 1000 characters.");
    }
}

public sealed class EscalateTicketCommandHandler : IRequestHandler<EscalateTicketCommand, EscalateTicketResult>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IChatRepository _chatRepository;

    public EscalateTicketCommandHandler(ITicketRepository ticketRepository, IChatRepository chatRepository)
    {
        _ticketRepository = ticketRepository;
        _chatRepository = chatRepository;
    }

    public async Task<EscalateTicketResult> Handle(EscalateTicketCommand request, CancellationToken ct)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);
        if (ticket is null || ticket.AssignedAgentId != request.AgentId || ticket.Status != TicketStatus.InProgress)
            throw new KeyNotFoundException($"Ticket #{request.TicketId} was not found or is not currently active for you.");

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Escalation reason is required.", nameof(request.Reason));
        var escalationReason = request.Reason.Trim();

        var escalation = ticket.EscalateToQueue(escalationReason);
        await _ticketRepository.AddEscalationAsync(escalation, ct);

        var chatMessage = new ChatMessage(request.TicketId, request.AgentId, escalationReason);
        await _chatRepository.AddMessageAsync(chatMessage, ct);

        await _ticketRepository.SaveChangesAsync(ct);

        return new EscalateTicketResult(ticket.Id, ticket.DepartmentId, escalationReason, escalation.EscalatedAt);
    }
}

public sealed record CloseAgentTicketCommand(int TicketId, int AgentId) : IRequest;

public sealed class CloseAgentTicketCommandHandler : IRequestHandler<CloseAgentTicketCommand>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketNotificationService _notificationService;

    public CloseAgentTicketCommandHandler(
        ITicketRepository ticketRepository,
        ITicketNotificationService notificationService)
    {
        _ticketRepository = ticketRepository;
        _notificationService = notificationService;
    }

    public async Task Handle(CloseAgentTicketCommand request, CancellationToken ct)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);
        if (ticket is null || ticket.AssignedAgentId != request.AgentId)
            throw new KeyNotFoundException($"Ticket #{request.TicketId} was not found or not assigned to you.");

        if (ticket.Status != TicketStatus.Resolved)
            throw new InvalidOperationException("Ticket must be marked as Resolved before it can be closed by an agent.");

        ticket.ChangeStatus(TicketStatus.Closed);
        await _ticketRepository.SaveChangesAsync(ct);
        await _notificationService.NotifyTicketClosedAsync(ticket, ct);
    }
}
