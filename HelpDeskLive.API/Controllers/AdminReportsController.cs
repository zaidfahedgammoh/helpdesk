using System.Data;
using Dapper;
using HelpDeskLive.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
namespace HelpDeskLive.API.Controllers;
[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Admin")]
public sealed class AdminReportsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    public AdminReportsController(IConfiguration configuration) => _configuration = configuration;

    [HttpGet("department-ticket-status")]
    public async Task<IActionResult> DepartmentTicketStatus(CancellationToken ct)
    {
        await using var connection = new SqlConnection(GetConnectionString());
        var command = new CommandDefinition("dbo.GetDepartmentTicketStatusReport",
            commandType: CommandType.StoredProcedure, cancellationToken: ct);
        var rows = await connection.QueryAsync<DepartmentTicketStatusReport>(command);
        return Ok(rows);
    }

    [HttpGet("status-summary")]
    public async Task<IActionResult> StatusSummary(CancellationToken ct)
    {
        var rows = new List<TicketStatusSummaryReport>();
        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Status, COUNT_BIG(*) AS TicketCount
            FROM dbo.Tickets
            GROUP BY Status
            ORDER BY Status;
            """;
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var rawStatus = reader.GetInt32(0);
            rows.Add(new TicketStatusSummaryReport(
                rawStatus,
                Enum.IsDefined(typeof(TicketStatus), rawStatus)
                    ? ((TicketStatus)rawStatus).ToString() : "Unknown",
                reader.GetInt64(1)));
        }
        return Ok(rows);
    }

    private string GetConnectionString() =>
        _configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Database connection is not configured.");
}
public sealed record DepartmentTicketStatusReport(
    int DepartmentId, string DepartmentName, int? Status,
    int TicketCount, double? AverageHoursToClose);
public sealed record TicketStatusSummaryReport(
    int Status, string StatusName, long TicketCount);
