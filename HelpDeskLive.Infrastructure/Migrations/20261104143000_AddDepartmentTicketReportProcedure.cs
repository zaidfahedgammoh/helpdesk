using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace HelpDeskLive.Infrastructure.Migrations;
[DbContext(typeof(HelpDeskLive.Infrastructure.Data.HelpDeskDbContext))]
[Migration("20261104143000_AddDepartmentTicketReportProcedure")]
public sealed class AddDepartmentTicketReportProcedure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE dbo.GetDepartmentTicketStatusReport
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT d.Id AS DepartmentId, d.Name AS DepartmentName,
                       t.Status AS Status, COUNT(t.Id) AS TicketCount,
                       AVG(CASE WHEN t.ClosedAt IS NOT NULL
                           THEN CAST(DATEDIFF_BIG(SECOND, t.CreatedAt, t.ClosedAt) AS float) / 3600.0
                           ELSE NULL END) AS AverageHoursToClose
                FROM dbo.Departments AS d
                LEFT JOIN dbo.Tickets AS t ON t.DepartmentId = d.Id
                GROUP BY d.Id, d.Name, t.Status
                ORDER BY d.Name, t.Status;
            END
            """);
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.GetDepartmentTicketStatusReport;");
}