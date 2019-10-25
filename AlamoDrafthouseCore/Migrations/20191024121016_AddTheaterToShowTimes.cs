using Microsoft.EntityFrameworkCore.Migrations;

namespace MaguSoft.ComeAndTicket.Core.Migrations
{
    public partial class AddTheaterToShowTimes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TheaterUrl",
                table: "ShowTimes",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShowTimes_TheaterUrl",
                table: "ShowTimes",
                column: "TheaterUrl");

            migrationBuilder.AddForeignKey(
                name: "FK_ShowTimes_Theaters_TheaterUrl",
                table: "ShowTimes",
                column: "TheaterUrl",
                principalTable: "Theaters",
                principalColumn: "Url",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShowTimes_Theaters_TheaterUrl",
                table: "ShowTimes");

            migrationBuilder.DropIndex(
                name: "IX_ShowTimes_TheaterUrl",
                table: "ShowTimes");

            migrationBuilder.DropColumn(
                name: "TheaterUrl",
                table: "ShowTimes");
        }
    }
}
