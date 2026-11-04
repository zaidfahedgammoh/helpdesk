namespace HelpDeskLive.Domain.Entities;
public sealed class TicketEscalation
{
    public int Id { get; private set; }
    public int TicketId { get; private set; }
    public int AgentId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    private TicketEscalation() { }
    public TicketEscalation(int ticketId, int agentId, string reason)
    {
        if (ticketId <= 0) throw new ArgumentOutOfRangeException(nameof(ticketId));
        if (agentId <= 0) throw new ArgumentOutOfRangeException(nameof(agentId));
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000)
            throw new ArgumentException("An escalation reason of 1 to 1000 characters is required.", nameof(reason));
        TicketId = ticketId;
        AgentId = agentId;
        Reason = reason.Trim();
        CreatedAt = DateTime.UtcNow;
    }
}