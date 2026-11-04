using HelpDeskLive.Application.Dashboard.Admin;

namespace HelpDeskLive.Application.Abstractions;

public interface IAdminReportingService
{
    Task<IReadOnlyList<DepartmentTicketStatsDto>> GetDepartmentTicketStatsViaLinqAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AgentTicketStatsDto>> GetAgentTicketStatsViaDapperAsync(CancellationToken ct = default);
    Task<EscalatedTicketStatsDto> GetEscalatedTicketStatsViaStoredProcedureAsync(CancellationToken ct = default);
    Task<ResolutionClosingTimeStatsDto> GetPerformanceStatsViaAdoNetAsync(CancellationToken ct = default);
}
