using HelpDeskLive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDeskLive.Infrastructure.Migrations;

[DbContext(typeof(HelpDeskDbContext))]
[Migration("20261103120000_CustomTicketApprovalAndAgentDepartments")]
public partial class CustomTicketApprovalAndAgentDepartments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "DepartmentId",
            table: "Tickets",
            type: "int",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "int");

        migrationBuilder.AlterColumn<int>(
            name: "Priority",
            table: "Tickets",
            type: "int",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "int");

        migrationBuilder.AddColumn<int>(
            name: "ApprovalStatus",
            table: "Tickets",
            type: "int",
            nullable: false,
            defaultValue: 2);

        migrationBuilder.AddColumn<int>(
            name: "DepartmentId",
            table: "Users",
            type: "int",
            nullable: true);

        migrationBuilder.Sql("UPDATE Users SET DepartmentId = (SELECT TOP 1 Id FROM Departments ORDER BY Id) WHERE Role = 2 AND EXISTS (SELECT 1 FROM Departments)");

        migrationBuilder.CreateIndex(
            name: "IX_Users_DepartmentId",
            table: "Users",
            column: "DepartmentId");

        migrationBuilder.AddForeignKey(
            name: "FK_Users_Departments_DepartmentId",
            table: "Users",
            column: "DepartmentId",
            principalTable: "Departments",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Users_Departments_DepartmentId", table: "Users");
        migrationBuilder.DropIndex(name: "IX_Users_DepartmentId", table: "Users");
        migrationBuilder.DropColumn(name: "DepartmentId", table: "Users");
        migrationBuilder.DropColumn(name: "ApprovalStatus", table: "Tickets");

        migrationBuilder.AlterColumn<int>(
            name: "DepartmentId",
            table: "Tickets",
            type: "int",
            nullable: false,
            defaultValue: 1,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "Priority",
            table: "Tickets",
            type: "int",
            nullable: false,
            defaultValue: 1,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);
    }
}
