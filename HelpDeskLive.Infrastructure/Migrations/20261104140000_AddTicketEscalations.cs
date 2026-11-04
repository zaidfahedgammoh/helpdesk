using HelpDeskLive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace HelpDeskLive.Infrastructure.Migrations;
[DbContext(typeof(HelpDeskDbContext))]
[Migration("20261104140000_AddTicketEscalations")]
public sealed class AddTicketEscalations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TicketEscalations",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                TicketId = table.Column<int>(type: "int", nullable: false),
                AgentId = table.Column<int>(type: "int", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TicketEscalations", x => x.Id);
                table.ForeignKey("FK_TicketEscalations_Tickets_TicketId", x => x.TicketId, "Tickets", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_TicketEscalations_Users_AgentId", x => x.AgentId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(name: "IX_TicketEscalations_TicketId_CreatedAt", table: "TicketEscalations", columns: new[] { "TicketId", "CreatedAt" });
        migrationBuilder.CreateIndex(name: "IX_TicketEscalations_AgentId", table: "TicketEscalations", column: "AgentId");
    }
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "TicketEscalations");
}