
namespace HelpDeskLive.MVC.Models;

public sealed class AdminDashboardViewModel
{
    public int TotalTickets { get; set; }

    public int OpenTickets { get; set; }

    public int InProgressTickets { get; set; }

    public int ResolvedTickets { get; set; }

    public int ClosedTickets { get; set; }

    public int TotalAgents { get; set; }

    public int TotalCustomers { get; set; }
}
