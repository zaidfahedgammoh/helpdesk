using HelpDeskLive.Domain.Enums;

namespace HelpDeskLive.Domain.Entities;

public class Ticket
{
    public int Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TicketStatus Status { get; private set; }
    public TicketPriority? Priority { get; private set; }
    public int? DepartmentId { get; private set; }
    public int CustomerId { get; private set; }
    public int? AssignedAgentId { get; private set; }
    public TicketApprovalStatus ApprovalStatus { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    private readonly List<TicketEscalation> _escalations = new();
    public IReadOnlyCollection<TicketEscalation> Escalations => _escalations.AsReadOnly();

    private Ticket() { }

    public Ticket(string title, string description, TicketPriority priority, int departmentId, int customerId)
    {
        ValidateText(title, description, customerId);
        if (!Enum.IsDefined(priority)) throw new ArgumentOutOfRangeException(nameof(priority));
        if (departmentId <= 0) throw new ArgumentOutOfRangeException(nameof(departmentId));

        Title = title.Trim();
        Description = description.Trim();
        Priority = priority;
        DepartmentId = departmentId;
        CustomerId = customerId;
        Status = TicketStatus.Open;
        ApprovalStatus = TicketApprovalStatus.Approved;
        CreatedAt = DateTime.UtcNow;
    }

    public static Ticket CreateCustomRequest(string title, string description, int customerId)
    {
        ValidateText(title, description, customerId);
        return new Ticket
        {
            Title = title.Trim(),
            Description = description.Trim(),
            CustomerId = customerId,
            Status = TicketStatus.Open,
            ApprovalStatus = TicketApprovalStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Approve(TicketPriority priority, int departmentId)
    {
        if (ApprovalStatus != TicketApprovalStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be approved.");
        if (!Enum.IsDefined(priority)) throw new ArgumentOutOfRangeException(nameof(priority));
        if (departmentId <= 0) throw new ArgumentOutOfRangeException(nameof(departmentId));

        Priority = priority;
        DepartmentId = departmentId;
        ApprovalStatus = TicketApprovalStatus.Approved;
    }

    public void Reject()
    {
        if (ApprovalStatus != TicketApprovalStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be rejected.");
        ApprovalStatus = TicketApprovalStatus.Rejected;
    }

    public void ChangeStatus(TicketStatus newStatus)
    {
        if (ApprovalStatus != TicketApprovalStatus.Approved)
            throw new InvalidOperationException("Only approved tickets can change status.");
        bool valid = (Status, newStatus) switch
        {
            (TicketStatus.Open, TicketStatus.InProgress) => true,
            (TicketStatus.Escalated, TicketStatus.InProgress) => true,
            (TicketStatus.InProgress, TicketStatus.Resolved) => true,
            (TicketStatus.Resolved, TicketStatus.Closed) => true,
            _ => false
        };
        if (!valid) throw new InvalidOperationException($"Cannot change ticket status from {Status} to {newStatus}.");
        Status = newStatus;
        if (newStatus == TicketStatus.Closed) ClosedAt = DateTime.UtcNow;
    }

    public TicketEscalation EscalateToQueue(string? reason = null)
    {
        if (ApprovalStatus != TicketApprovalStatus.Approved || Status != TicketStatus.InProgress)
            throw new InvalidOperationException("Only an active approved ticket can be escalated.");
        var previousAgentId = AssignedAgentId ?? 0;
        if (previousAgentId <= 0)
            throw new InvalidOperationException("An agent must be assigned to escalate this ticket.");

        var escalationReason = string.IsNullOrWhiteSpace(reason)
            ? "Ticket escalated by the assigned agent and returned to the department escalation queue."
            : reason.Trim();

        Status = TicketStatus.Escalated;
        AssignedAgentId = null;
        var escalation = new TicketEscalation(Id, previousAgentId, escalationReason);
        _escalations.Add(escalation);
        return escalation;
    }

    public void CloseByCustomer()
    {
        if (ApprovalStatus != TicketApprovalStatus.Approved ||
            Status is TicketStatus.Closed or TicketStatus.Resolved)
            throw new InvalidOperationException("This ticket cannot be closed by the customer.");
        Status = TicketStatus.Closed;
        AssignedAgentId = null;
        ClosedAt = DateTime.UtcNow;
    }

    public void AssignToAgent(int agentId)
    {
        if (ApprovalStatus != TicketApprovalStatus.Approved || DepartmentId is null)
            throw new InvalidOperationException("Only approved tickets with a department can be assigned.");
        if (agentId <= 0) throw new ArgumentOutOfRangeException(nameof(agentId));
        if (Status is not (TicketStatus.Open or TicketStatus.Escalated))
            throw new InvalidOperationException("Only open or escalated tickets can be assigned.");
        if (AssignedAgentId is not null && AssignedAgentId != agentId)
            throw new InvalidOperationException("This ticket is already assigned to another agent.");

        if (Status == TicketStatus.Escalated)
        {
            var pendingEscalation = _escalations.LastOrDefault(e => e.NewAgentId == null);
            pendingEscalation?.AssignNewAgent(agentId);
        }

        AssignedAgentId = agentId;
    }

    private static void ValidateText(string title, string description, int customerId)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Ticket title is required.", nameof(title));
        if (title.Length > 200) throw new ArgumentException("Ticket title cannot exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Ticket description is required.", nameof(description));
        if (description.Length > 5000) throw new ArgumentException("Ticket description cannot exceed 5000 characters.");
        if (customerId <= 0) throw new ArgumentOutOfRangeException(nameof(customerId));
    }
}
