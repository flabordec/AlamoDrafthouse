using Microsoft.EntityFrameworkCore.Migrations;

namespace MaguSoft.ComeAndTicket.Core.Migrations
{
    public partial class RemoveMovieTheaters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Movies_Theaters_TheaterUrl",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_TheaterUrl",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "TheaterUrl",
                table: "Movies");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TheaterUrl",
                table: "Movies",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Movies_TheaterUrl",
                table: "Movies",
                column: "TheaterUrl");

            migrationBuilder.AddForeignKey(
                name: "FK_Movies_Theaters_TheaterUrl",
                table: "Movies",
                column: "TheaterUrl",
                principalTable: "Theaters",
                principalColumn: "Url",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
