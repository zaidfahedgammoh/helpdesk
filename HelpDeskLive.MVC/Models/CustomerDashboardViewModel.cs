namespace HelpDeskLive.MVC.Models;

public sealed class CustomerDashboardViewModel
{
    public IReadOnlyList<CustomerTicketViewModel> Tickets { get; init; } = Array.Empty<CustomerTicketViewModel>();
    public IReadOnlyList<CustomerIssueTypeViewModel> IssueTypes { get; init; } = Array.Empty<CustomerIssueTypeViewModel>();
    public int TotalTickets => Tickets.Count;
    public int OpenTickets => Tickets.Count(ticket => ticket.Status == 1 && ticket.ApprovalStatus == 2);
    public int PendingApprovalTickets => Tickets.Count(ticket => ticket.ApprovalStatus == 1);
    public int InProgressTickets => Tickets.Count(ticket => ticket.Status == 2);
    public int ResolvedTickets => Tickets.Count(ticket => ticket.Status == 3);
    public int ClosedTickets => Tickets.Count(ticket => ticket.Status == 4);
}

public sealed class CustomerTicketViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public int Status { get; init; }
    public int? Priority { get; init; }
    public int ApprovalStatus { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class CustomerIssueTypeViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int DefaultPriority { get; init; }
}

public sealed class CreateCustomerTicketViewModel
{
    public int? IssueTypeId { get; set; }
    public string? CustomTitle { get; set; }
    public string Description { get; set; } = string.Empty;
}
