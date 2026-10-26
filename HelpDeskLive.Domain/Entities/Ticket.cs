
using HelpDeskLive.Domain.Enums;

namespace HelpDeskLive.Domain.Entities;

public class Ticket
{
    public int Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public TicketStatus Status { get; private set; }

    public TicketPriority Priority { get; private set; }

    public int DepartmentId { get; private set; }

    public int CustomerId { get; private set; }

    public int? AssignedAgentId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ClosedAt { get; private set; }
    private Ticket()
    {
    }

    public Ticket(
        string title,
        string description,
        TicketPriority priority,
        int departmentId,
        int customerId)
    {
        Title = title;
        Description = description;
        Priority = priority;
        DepartmentId = departmentId;
        CustomerId = customerId;

        Status = TicketStatus.Open;
        CreatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(TicketStatus newStatus)
    {
        bool isValidTransition = (Status, newStatus) switch
        {
            (TicketStatus.Open, TicketStatus.InProgress) => true,
            (TicketStatus.InProgress, TicketStatus.Resolved) => true,
            (TicketStatus.Resolved, TicketStatus.Closed) => true,
            _ => false
        };

        if (!isValidTransition)
        {
            throw new InvalidOperationException(
                $"Cannot change ticket status from {Status} to {newStatus}.");
        }

        Status = newStatus;

        if (newStatus == TicketStatus.Closed)
        {
            ClosedAt = DateTime.UtcNow;
        }
    }

    public void AssignToAgent(int agentId)
    {
        if (agentId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(agentId),
                "Agent ID must be greater than zero.");
        }

        if (Status != TicketStatus.Open)
        {
            throw new InvalidOperationException(
                "Only open tickets can be assigned to an agent.");
        }

        AssignedAgentId = agentId;
    }

}
