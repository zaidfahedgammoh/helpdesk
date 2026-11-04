using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Enums;
using MediatR;

namespace HelpDeskLive.Application.Tickets.Admin;

public sealed record PendingApprovalTicketDto(
    int Id,
    string Title,
    string Description,
    int CustomerId,
    DateTime CreatedAt
);

public sealed record GetPendingApprovalsQuery : IRequest<IReadOnlyList<PendingApprovalTicketDto>>;

public sealed class GetPendingApprovalsQueryHandler : IRequestHandler<GetPendingApprovalsQuery, IReadOnlyList<PendingApprovalTicketDto>>
{
    private readonly ITicketRepository _ticketRepository;

    public GetPendingApprovalsQueryHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<IReadOnlyList<PendingApprovalTicketDto>> Handle(GetPendingApprovalsQuery request, CancellationToken ct)
    {
        var tickets = await _ticketRepository.GetPendingApprovalTicketsAsync(ct);
        return tickets.Select(t => new PendingApprovalTicketDto(
            t.Id,
            t.Title,
            t.Description,
            t.CustomerId,
            t.CreatedAt)).ToList();
    }
}

public sealed record ApproveTicketCommand(int TicketId, TicketPriority Priority, int DepartmentId) : IRequest;

public sealed class ApproveTicketCommandHandler : IRequestHandler<ApproveTicketCommand>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public ApproveTicketCommandHandler(ITicketRepository ticketRepository, IDepartmentRepository departmentRepository)
    {
        _ticketRepository = ticketRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task Handle(ApproveTicketCommand request, CancellationToken ct)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);
        if (ticket is null)
            throw new KeyNotFoundException($"Ticket #{request.TicketId} was not found.");

        var departmentExists = await _departmentRepository.ExistsAsync(request.DepartmentId, ct);
        if (!departmentExists)
            throw new ArgumentException("Select a valid department.");

        ticket.Approve(request.Priority, request.DepartmentId);
        await _ticketRepository.SaveChangesAsync(ct);
    }
}

public sealed record RejectTicketCommand(int TicketId) : IRequest;

public sealed class RejectTicketCommandHandler : IRequestHandler<RejectTicketCommand>
{
    private readonly ITicketRepository _ticketRepository;

    public RejectTicketCommandHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task Handle(RejectTicketCommand request, CancellationToken ct)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);
        if (ticket is null)
            throw new KeyNotFoundException($"Ticket #{request.TicketId} was not found.");

        ticket.Reject();
        await _ticketRepository.SaveChangesAsync(ct);
    }
}
