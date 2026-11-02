
namespace HelpDeskLive.MVC.Models;

public sealed class AgentDashboardViewModel
{
    public int AssignedTickets { get; set; }

    public int OpenTickets { get; set; }

    public int InProgressTickets { get; set; }

    public int ResolvedTickets { get; set; }

    public int ClosedTickets { get; set; }
}
