using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDeskLive.Infrastructure.Migrations
{
    public partial class AddTicketEscalationHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'dbo.TicketEscalations', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[TicketEscalations] (
                        [Id] int NOT NULL IDENTITY(1, 1),
                        [TicketId] int NOT NULL,
                        [PreviousAgentId] int NOT NULL,
                        [NewAgentId] int NULL,
                        [Reason] nvarchar(1000) NOT NULL,
                        [EscalatedAt] datetime2 NOT NULL,
                        [ReassignedAt] datetime2 NULL,
                        CONSTRAINT [PK_TicketEscalations] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_TicketEscalations_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [dbo].[Tickets] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_TicketEscalations_Users_NewAgentId] FOREIGN KEY ([NewAgentId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_TicketEscalations_Users_PreviousAgentId] FOREIGN KEY ([PreviousAgentId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION
                    );

                    CREATE INDEX [IX_TicketEscalations_NewAgentId] ON [dbo].[TicketEscalations] ([NewAgentId]);
                    CREATE INDEX [IX_TicketEscalations_PreviousAgentId] ON [dbo].[TicketEscalations] ([PreviousAgentId]);
                    CREATE INDEX [IX_TicketEscalations_TicketId] ON [dbo].[TicketEscalations] ([TicketId]);
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID(N'dbo.TicketEscalations', N'U') IS NOT NULL DROP TABLE [dbo].[TicketEscalations];");
        }
    }
}
