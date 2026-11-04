using System.Data;
using Dapper;
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Application.Dashboard.Admin;
using HelpDeskLive.Domain.Enums;
using HelpDeskLive.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HelpDeskLive.Infrastructure.Services;

public sealed class AdminReportingService : IAdminReportingService
{
    private readonly HelpDeskDbContext _dbContext;
    private readonly string _connectionString;
    private static bool _storedProcedureEnsured;

    public AdminReportingService(HelpDeskDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _connectionString = dbContext.Database.GetConnectionString()
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? string.Empty;
    }

    public async Task<IReadOnlyList<DepartmentTicketStatsDto>> GetDepartmentTicketStatsViaLinqAsync(CancellationToken ct = default)
    {
        var departments = await _dbContext.Departments.AsNoTracking().ToListAsync(ct);
        var tickets = await _dbContext.Tickets.AsNoTracking()
            .Where(t => t.ApprovalStatus == TicketApprovalStatus.Approved)
            .ToListAsync(ct);

        var list = new List<DepartmentTicketStatsDto>();
        foreach (var dept in departments)
        {
            var deptTickets = tickets.Where(t => t.DepartmentId == dept.Id).ToList();
            list.Add(new DepartmentTicketStatsDto(
                dept.Id,
                dept.Name,
                deptTickets.Count,
                deptTickets.Count(t => t.Status == TicketStatus.Open),
                deptTickets.Count(t => t.Status == TicketStatus.InProgress),
                deptTickets.Count(t => t.Status == TicketStatus.Resolved),
                deptTickets.Count(t => t.Status == TicketStatus.Closed),
                deptTickets.Count(t => t.Status == TicketStatus.Escalated)
            ));
        }

        return list.OrderBy(d => d.DepartmentName).ToList();
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
        await EnsureStoredProcedureExistsAsync(ct);

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
                select (double?)EF.Functions.DateDiffMinute(esc.EscalatedAt, t.ClosedAt ?? DateTime.UtcNow) / 60.0
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
        catch
        {
            var total = await _dbContext.TicketEscalations.CountAsync(ct);
            var active = await _dbContext.TicketEscalations.CountAsync(e => e.NewAgentId == null, ct);
            var reassigned = await _dbContext.TicketEscalations.CountAsync(e => e.NewAgentId != null, ct);
            return new EscalatedTicketStatsDto(total, active, reassigned, 0.0);
        }
    }

    public async Task<ResolutionClosingTimeStatsDto> GetPerformanceStatsViaAdoNetAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
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
                var isClosedAtNull = reader.IsDBNull(2);
                var closedAt = isClosedAtNull ? (DateTime?)null : reader.GetDateTime(2);
                var status = reader.GetInt32(3);

                if (status == 3)
                {
                    resolvedCount++;
                    var resTime = closedAt ?? DateTime.UtcNow;
                    totalResolvedMinutes += (resTime - createdAt).TotalMinutes;
                }
                else if (status == 4 && closedAt.HasValue)
                {
                    closedCount++;
                    totalClosedMinutes += (closedAt.Value - createdAt).TotalMinutes;
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
        catch
        {
            return new ResolutionClosingTimeStatsDto(0, 0, 0, 0);
        }
    }

    private async Task EnsureStoredProcedureExistsAsync(CancellationToken ct)
    {
        if (_storedProcedureEnsured) return;

        const string sql = @"
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GetEscalationStats]') AND type in (N'P', N'PC'))
            BEGIN
                EXEC(N'CREATE PROCEDURE [dbo].[sp_GetEscalationStats] AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT 
                        COUNT(*) AS TotalEscalations,
                        ISNULL(SUM(CASE WHEN NewAgentId IS NULL THEN 1 ELSE 0 END), 0) AS CurrentlyEscalatedTickets,
                        ISNULL(SUM(CASE WHEN NewAgentId IS NOT NULL THEN 1 ELSE 0 END), 0) AS ReassignedEscalations
                    FROM TicketEscalations;
                END')
            END";

        try
        {
            await _dbContext.Database.ExecuteSqlRawAsync(sql, ct);
            _storedProcedureEnsured = true;
        }
        catch
        {
        }
    }

    private sealed class EscalationProcResult
    {
        public int TotalEscalations { get; set; }
        public int CurrentlyEscalatedTickets { get; set; }
        public int ReassignedEscalations { get; set; }
    }
}
