
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
    private Ticket() { }

    public Ticket(
        string title,
        string description,
        TicketPriority priority,
        int departmentId,
        int customerId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Ticket title is required.", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Ticket description is required.", nameof(description));

        if (!Enum.IsDefined(priority))
            throw new ArgumentOutOfRangeException(nameof(priority));

        if (departmentId <= 0)
            throw new ArgumentOutOfRangeException(nameof(departmentId));

        if (customerId <= 0)
            throw new ArgumentOutOfRangeException(nameof(customerId));

        Title = title.Trim();
        Description = description.Trim();
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
            ClosedAt = DateTime.UtcNow;
    }

    public void AssignToAgent(int agentId)
    {
        if (agentId <= 0)
            throw new ArgumentOutOfRangeException(nameof(agentId));

        if (Status != TicketStatus.Open)
        {
            throw new InvalidOperationException(
                "Only open tickets can be assigned to an agent.");
        }

        AssignedAgentId = agentId;
    }
}
