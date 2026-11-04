using HelpDeskLive.Application.Abstractions;
using MediatR;

namespace HelpDeskLive.Application.Tickets.Escalation;

public sealed record TicketEscalationDto(
    int Id,
    int TicketId,
    int PreviousAgentId,
    string PreviousAgentName,
    int? NewAgentId,
    string? NewAgentName,
    string Reason,
    DateTime EscalatedAt,
    DateTime? ReassignedAt
);

public sealed record GetTicketEscalationHistoryQuery(int TicketId) : IRequest<IReadOnlyList<TicketEscalationDto>>;

public sealed class GetTicketEscalationHistoryQueryHandler : IRequestHandler<GetTicketEscalationHistoryQuery, IReadOnlyList<TicketEscalationDto>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;

    public GetTicketEscalationHistoryQueryHandler(ITicketRepository ticketRepository, IUserRepository userRepository)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<TicketEscalationDto>> Handle(GetTicketEscalationHistoryQuery request, CancellationToken ct)
    {
        var escalations = await _ticketRepository.GetEscalationsByTicketIdAsync(request.TicketId, ct);
        var dtos = new List<TicketEscalationDto>(escalations.Count);

        foreach (var esc in escalations)
        {
            var prevAgent = await _userRepository.GetByIdAsync(esc.PreviousAgentId, ct);
            var newAgent = esc.NewAgentId.HasValue ? await _userRepository.GetByIdAsync(esc.NewAgentId.Value, ct) : null;

            dtos.Add(new TicketEscalationDto(
                esc.Id,
                esc.TicketId,
                esc.PreviousAgentId,
                prevAgent?.Name ?? $"Agent #{esc.PreviousAgentId}",
                esc.NewAgentId,
                newAgent?.Name,
                esc.Reason,
                esc.EscalatedAt,
                esc.ReassignedAt));
        }

        return dtos;
    }
}
