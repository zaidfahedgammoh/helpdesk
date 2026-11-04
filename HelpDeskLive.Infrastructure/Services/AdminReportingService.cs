using System.Data;
using Dapper;
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Application.Dashboard.Admin;
using HelpDeskLive.Domain.Enums;
using HelpDeskLive.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HelpDeskLive.Infrastructure.Services;

public sealed class AdminReportingService : IAdminReportingService
{
    private readonly HelpDeskDbContext _dbContext;
    private readonly string _connectionString;
    private readonly ILogger<AdminReportingService> _logger;

    public AdminReportingService(
        HelpDeskDbContext dbContext,
        IConfiguration configuration,
        ILogger<AdminReportingService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        string? connectionString = null;
        try
        {
            if (dbContext.Database.IsRelational())
            {
                connectionString = dbContext.Database.GetConnectionString();
            }
        }
        catch { }

        _connectionString = connectionString
            ?? (configuration != null ? configuration.GetConnectionString("DefaultConnection") : null)
            ?? string.Empty;
    }

    public async Task<IReadOnlyList<DepartmentTicketStatsDto>> GetDepartmentTicketStatsViaLinqAsync(CancellationToken ct = default)
    {
        return await _dbContext.Departments.AsNoTracking()
            .Select(dept => new DepartmentTicketStatsDto(
                dept.Id,
                dept.Name,
                _dbContext.Tickets.Count(t => t.DepartmentId == dept.Id && t.ApprovalStatus == TicketApprovalStatus.Approved),
                _dbContext.Tickets.Count(t => t.DepartmentId == dept.Id && t.ApprovalStatus == TicketApprovalStatus.Approved && t.Status == TicketStatus.Open),
                _dbContext.Tickets.Count(t => t.DepartmentId == dept.Id && t.ApprovalStatus == TicketApprovalStatus.Approved && t.Status == TicketStatus.InProgress),
                _dbContext.Tickets.Count(t => t.DepartmentId == dept.Id && t.ApprovalStatus == TicketApprovalStatus.Approved && t.Status == TicketStatus.Resolved),
                _dbContext.Tickets.Count(t => t.DepartmentId == dept.Id && t.ApprovalStatus == TicketApprovalStatus.Approved && t.Status == TicketStatus.Closed),
                _dbContext.Tickets.Count(t => t.DepartmentId == dept.Id && t.ApprovalStatus == TicketApprovalStatus.Approved && t.Status == TicketStatus.Escalated)
            ))
            .OrderBy(d => d.DepartmentName)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AgentTicketStatsDto>> GetAgentTicketStatsViaDapperAsync(CancellationToken ct = default)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        const string sql = @"
            SELECT 
                u.Id AS AgentId,
                u.Name AS AgentName,
                COALESCE(d.Name, 'None') AS DepartmentName,
                SUM(CASE WHEN t.Status = 2 AND t.ApprovalStatus = 2 THEN 1 ELSE 0 END) AS ActiveTickets,
                SUM(CASE WHEN t.Status = 3 AND t.ApprovalStatus = 2 THEN 1 ELSE 0 END) AS ResolvedTickets,
                SUM(CASE WHEN t.Status = 4 AND t.ApprovalStatus = 2 THEN 1 ELSE 0 END) AS ClosedTickets,
                COUNT(t.Id) AS TotalHandled
            FROM Users u
            LEFT JOIN Departments d ON u.DepartmentId = d.Id
            LEFT JOIN Tickets t ON t.AssignedAgentId = u.Id
            WHERE u.Role = 2
            GROUP BY u.Id, u.Name, d.Name
            ORDER BY u.Name";

        var results = await connection.QueryAsync<AgentTicketStatsDto>(new CommandDefinition(sql, cancellationToken: ct));
        return results.ToList();
    }

    public async Task<EscalatedTicketStatsDto> GetEscalatedTicketStatsViaStoredProcedureAsync(CancellationToken ct = default)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        try
        {
            var result = await connection.QueryFirstOrDefaultAsync<EscalationProcResult>(
                new CommandDefinition("sp_GetEscalationStats", commandType: CommandType.StoredProcedure, cancellationToken: ct));

            var durations = await (
                from esc in _dbContext.TicketEscalations.AsNoTracking()
                join t in _dbContext.Tickets.AsNoTracking() on esc.TicketId equals t.Id
                where t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed
                select (double?)EF.Functions.DateDiffMinute(esc.EscalatedAt, t.ResolvedAt ?? t.ClosedAt ?? DateTime.UtcNow) / 60.0
            ).ToListAsync(ct);

            var avgHoursAfterEscalation = durations.Count != 0 && durations.Any(d => d.HasValue)
                ? durations.Where(d => d.HasValue).Average(d => d!.Value)
                : 0.0;

            return new EscalatedTicketStatsDto(
                result?.TotalEscalations ?? 0,
                result?.CurrentlyEscalatedTickets ?? 0,
                result?.ReassignedEscalations ?? 0,
                Math.Round(avgHoursAfterEscalation, 1)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing sp_GetEscalationStats for escalation statistics report.");
            var total = await _dbContext.TicketEscalations.CountAsync(ct);
            var active = await _dbContext.Tickets.CountAsync(t => t.Status == TicketStatus.Escalated, ct);
            var reassigned = await _dbContext.TicketEscalations.CountAsync(e => e.NewAgentId != null, ct);
            return new EscalatedTicketStatsDto(total, active, reassigned, 0.0);
        }
    }

    public async Task<ResolutionClosingTimeStatsDto> GetPerformanceStatsViaAdoNetAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            _logger.LogWarning("Connection string is missing for ADO.NET performance stats query.");
            return new ResolutionClosingTimeStatsDto(0, 0, 0, 0);
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);

            const string sql = @"
                SELECT 
                    Id,
                    CreatedAt,
                    ResolvedAt,
                    ClosedAt,
                    Status
                FROM Tickets
                WHERE ApprovalStatus = 2 AND (Status = 3 OR Status = 4);";

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(ct);

            var totalResolvedMinutes = 0.0;
            var resolvedCount = 0;
            var totalClosedMinutes = 0.0;
            var closedCount = 0;

            while (await reader.ReadAsync(ct))
            {
                var createdAt = reader.GetDateTime(1);
                var isResolvedAtNull = reader.IsDBNull(2);
                var resolvedAt = isResolvedAtNull ? (DateTime?)null : reader.GetDateTime(2);
                var isClosedAtNull = reader.IsDBNull(3);
                var closedAt = isClosedAtNull ? (DateTime?)null : reader.GetDateTime(3);
                var status = reader.GetInt32(4);

                if (status == 3) // Resolved
                {
                    resolvedCount++;
                    var resTime = resolvedAt ?? closedAt ?? DateTime.UtcNow;
                    totalResolvedMinutes += (resTime - createdAt).TotalMinutes;
                }
                else if (status == 4) // Closed
                {
                    if (resolvedAt.HasValue)
                    {
                        resolvedCount++;
                        totalResolvedMinutes += (resolvedAt.Value - createdAt).TotalMinutes;
                    }
                    if (closedAt.HasValue)
                    {
                        closedCount++;
                        totalClosedMinutes += (closedAt.Value - createdAt).TotalMinutes;
                    }
                }
            }

            var avgResolutionHours = resolvedCount > 0 ? (totalResolvedMinutes / resolvedCount) / 60.0 : 0.0;
            var avgClosingHours = closedCount > 0 ? (totalClosedMinutes / closedCount) / 60.0 : 0.0;

            return new ResolutionClosingTimeStatsDto(
                Math.Round(avgResolutionHours, 1),
                Math.Round(avgClosingHours, 1),
                resolvedCount,
                closedCount
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating performance stats via ADO.NET.");
            throw;
        }
    }

    private sealed class EscalationProcResult
    {
        public int TotalEscalations { get; set; }
        public int CurrentlyEscalatedTickets { get; set; }
        public int ReassignedEscalations { get; set; }
    }
}
