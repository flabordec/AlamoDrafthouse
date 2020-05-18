using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace MaguSoft.ComeAndTicket.Core.Migrations
{
    public partial class UsersDevicesAndTitlesToWatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceNicknames",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MovieTitlesToWatch",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "HomeMarketUrl",
                table: "Users",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeviceNickname",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Value = table.Column<string>(nullable: true),
                    UserId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceNickname", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceNickname_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovieTitleToWatch",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Value = table.Column<string>(nullable: true),
                    UserId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieTitleToWatch", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovieTitleToWatch_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_HomeMarketUrl",
                table: "Users",
                column: "HomeMarketUrl");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceNickname_UserId",
                table: "DeviceNickname",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MovieTitleToWatch_UserId",
                table: "MovieTitleToWatch",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Markets_HomeMarketUrl",
                table: "Users",
                column: "HomeMarketUrl",
                principalTable: "Markets",
                principalColumn: "Url",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Markets_HomeMarketUrl",
                table: "Users");

            migrationBuilder.DropTable(
                name: "DeviceNickname");

            migrationBuilder.DropTable(
                name: "MovieTitleToWatch");

            migrationBuilder.DropIndex(
                name: "IX_Users_HomeMarketUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HomeMarketUrl",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "DeviceNicknames",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MovieTitlesToWatch",
                table: "Users",
                type: "text",
                nullable: true);
        }
    }
}
