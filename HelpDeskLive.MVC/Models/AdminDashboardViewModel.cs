namespace HelpDeskLive.MVC.Models;

public sealed class DepartmentTicketStatsViewModel
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int ClosedTickets { get; set; }
    public int EscalatedTickets { get; set; }
}

public sealed class AgentTicketStatsViewModel
{
    public int AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int ActiveTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int ClosedTickets { get; set; }
    public int TotalHandled { get; set; }
}

public sealed class EscalatedTicketStatsViewModel
{
    public int TotalEscalations { get; set; }
    public int CurrentlyEscalatedTickets { get; set; }
    public int ReassignedEscalations { get; set; }
    public double AverageResolutionAfterEscalationHours { get; set; }
}

public sealed class ResolutionClosingTimeStatsViewModel
{
    public double AverageResolutionTimeHours { get; set; }
    public double AverageClosingTimeHours { get; set; }
    public int TotalResolvedSample { get; set; }
    public int TotalClosedSample { get; set; }
}

public sealed class AdminDashboardViewModel
{
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int ClosedTickets { get; set; }
    public int TotalAgents { get; set; }
    public int TotalCustomers { get; set; }

    public List<DepartmentTicketStatsViewModel> DepartmentStats { get; set; } = [];
    public List<AgentTicketStatsViewModel> AgentStats { get; set; } = [];
    public EscalatedTicketStatsViewModel EscalatedStats { get; set; } = new();
    public ResolutionClosingTimeStatsViewModel PerformanceStats { get; set; } = new();
}
