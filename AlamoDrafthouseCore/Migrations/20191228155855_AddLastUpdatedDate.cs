using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MaguSoft.ComeAndTicket.Core.Migrations
{
    public partial class AddLastUpdatedDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "ShowTimes",
                nullable: false,
                defaultValue: DateTime.UtcNow);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "ShowTimes",
                nullable: false,
                defaultValue: DateTime.UtcNow);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Created",
                table: "ShowTimes");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "ShowTimes");
        }
    }
}
