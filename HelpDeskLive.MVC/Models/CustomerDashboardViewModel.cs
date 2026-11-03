namespace HelpDeskLive.MVC.Models;

public sealed class CustomerDashboardViewModel
{
    public IReadOnlyList<CustomerTicketViewModel> Tickets { get; init; }
        = Array.Empty<CustomerTicketViewModel>();

    public int TotalTickets => Tickets.Count;

    public int OpenTickets => Tickets.Count(
        ticket => ticket.Status == "Open");

    public int InProgressTickets => Tickets.Count(
        ticket => ticket.Status == "InProgress");

    public int ResolvedTickets => Tickets.Count(
        ticket => ticket.Status == "Resolved");

    public int ClosedTickets => Tickets.Count(
        ticket => ticket.Status == "Closed");
}

public sealed class CustomerTicketViewModel
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string Priority { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }
}