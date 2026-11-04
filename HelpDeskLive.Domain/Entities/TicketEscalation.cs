namespace HelpDeskLive.Domain.Entities;

public class TicketEscalation
{
    public int Id { get; private set; }
    public int TicketId { get; private set; }
    public int PreviousAgentId { get; private set; }
    public int? NewAgentId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime EscalatedAt { get; private set; }
    public DateTime? ReassignedAt { get; private set; }

    private TicketEscalation() { }

    public TicketEscalation(int ticketId, int previousAgentId, string reason)
    {
        if (ticketId < 0) throw new ArgumentOutOfRangeException(nameof(ticketId));
        if (previousAgentId <= 0) throw new ArgumentOutOfRangeException(nameof(previousAgentId));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Escalation reason is required.", nameof(reason));
        if (reason.Length > 1000) throw new ArgumentException("Escalation reason cannot exceed 1000 characters.", nameof(reason));

        TicketId = ticketId;
        PreviousAgentId = previousAgentId;
        Reason = reason.Trim();
        EscalatedAt = DateTime.UtcNow;
    }

    public void AssignNewAgent(int newAgentId)
    {
        if (newAgentId <= 0) throw new ArgumentOutOfRangeException(nameof(newAgentId));
        NewAgentId = newAgentId;
        ReassignedAt = DateTime.UtcNow;
    }
}
