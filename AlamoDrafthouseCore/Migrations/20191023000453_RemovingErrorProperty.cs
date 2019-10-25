using Microsoft.EntityFrameworkCore.Migrations;

namespace MaguSoft.ComeAndTicket.Core.Migrations
{
    public partial class RemovingErrorProperty : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MyTicketsStatus",
                table: "ShowTimes");

            migrationBuilder.AddColumn<int>(
                name: "TicketsStatus",
                table: "ShowTimes",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TicketsStatus",
                table: "ShowTimes");

            migrationBuilder.AddColumn<int>(
                name: "MyTicketsStatus",
                table: "ShowTimes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
