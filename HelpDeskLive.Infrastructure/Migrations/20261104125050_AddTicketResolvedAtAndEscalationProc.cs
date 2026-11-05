using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDeskLive.Infrastructure.Migrations
{
    public partial class AddTicketResolvedAtAndEscalationProc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF COL_LENGTH(N'dbo.Tickets', N'ResolvedAt') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[Tickets] ADD [ResolvedAt] datetime2 NULL;
                END
            ");

            migrationBuilder.Sql(@"
                UPDATE [dbo].[Tickets] 
                SET [ResolvedAt] = COALESCE([ClosedAt], [CreatedAt])
                WHERE [Status] IN (3, 4) AND [ResolvedAt] IS NULL;
            ");

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_GetEscalationStats];");

            migrationBuilder.Sql(@"
                IF COL_LENGTH(N'dbo.Tickets', N'ResolvedAt') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[Tickets] DROP COLUMN [ResolvedAt];
                END
            ");
        }
    }
}
