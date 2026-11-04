using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDeskLive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketResolvedAtAndEscalationProc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            // Backfill ResolvedAt for existing resolved/closed tickets safely
            migrationBuilder.Sql(@"
                UPDATE Tickets 
                SET ResolvedAt = COALESCE(ClosedAt, CreatedAt)
                WHERE Status IN (3, 4) AND ResolvedAt IS NULL;
            ");

            // Manage sp_GetEscalationStats stored procedure via migration
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_GetEscalationStats]
                AS
                BEGIN
                    SET NOCOUNT ON;
                    SELECT 
                        COUNT(e.Id) AS TotalEscalations,
                        ISNULL(SUM(CASE WHEN t.Status = 5 THEN 1 ELSE 0 END), 0) AS CurrentlyEscalatedTickets,
                        ISNULL(SUM(CASE WHEN e.NewAgentId IS NOT NULL OR t.Status <> 5 THEN 1 ELSE 0 END), 0) AS ReassignedEscalations
                    FROM TicketEscalations e
                    LEFT JOIN Tickets t ON e.TicketId = t.Id;
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_GetEscalationStats];");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "Tickets");
        }
    }
}
