using Microsoft.EntityFrameworkCore.Migrations;

namespace MaguSoft.ComeAndTicket.Core.Migrations
{
    public partial class UpdatingPropertyNames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Movies_Theaters_TheaterUrl",
                table: "Movies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Theaters",
                table: "Theaters");

            migrationBuilder.DropColumn(
                name: "TheaterUrl",
                table: "Theaters");

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Theaters",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Theaters",
                table: "Theaters",
                column: "Url");

            migrationBuilder.AddForeignKey(
                name: "FK_Movies_Theaters_TheaterUrl",
                table: "Movies",
                column: "TheaterUrl",
                principalTable: "Theaters",
                principalColumn: "Url",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Movies_Theaters_TheaterUrl",
                table: "Movies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Theaters",
                table: "Theaters");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "Theaters");

            migrationBuilder.AddColumn<string>(
                name: "TheaterUrl",
                table: "Theaters",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Theaters",
                table: "Theaters",
                column: "TheaterUrl");

            migrationBuilder.AddForeignKey(
                name: "FK_Movies_Theaters_TheaterUrl",
                table: "Movies",
                column: "TheaterUrl",
                principalTable: "Theaters",
                principalColumn: "TheaterUrl",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
