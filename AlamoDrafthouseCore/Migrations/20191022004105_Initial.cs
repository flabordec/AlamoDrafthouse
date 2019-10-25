using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MaguSoft.ComeAndTicket.Core.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Markets",
                columns: table => new
                {
                    Url = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Markets", x => x.Url);
                });

            migrationBuilder.CreateTable(
                name: "Theaters",
                columns: table => new
                {
                    TheaterUrl = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    MarketUrl = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Theaters", x => x.TheaterUrl);
                    table.ForeignKey(
                        name: "FK_Theaters_Markets_MarketUrl",
                        column: x => x.MarketUrl,
                        principalTable: "Markets",
                        principalColumn: "Url",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Movies",
                columns: table => new
                {
                    Title = table.Column<string>(nullable: false),
                    TheaterUrl = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movies", x => x.Title);
                    table.ForeignKey(
                        name: "FK_Movies_Theaters_TheaterUrl",
                        column: x => x.TheaterUrl,
                        principalTable: "Theaters",
                        principalColumn: "TheaterUrl",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShowTimes",
                columns: table => new
                {
                    TicketsUrl = table.Column<string>(nullable: false),
                    Date = table.Column<DateTime>(nullable: true),
                    MyTicketsStatus = table.Column<int>(nullable: false),
                    SeatsLeft = table.Column<int>(nullable: true),
                    MovieTitle = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowTimes", x => x.TicketsUrl);
                    table.ForeignKey(
                        name: "FK_ShowTimes_Movies_MovieTitle",
                        column: x => x.MovieTitle,
                        principalTable: "Movies",
                        principalColumn: "Title",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Movies_TheaterUrl",
                table: "Movies",
                column: "TheaterUrl");

            migrationBuilder.CreateIndex(
                name: "IX_ShowTimes_MovieTitle",
                table: "ShowTimes",
                column: "MovieTitle");

            migrationBuilder.CreateIndex(
                name: "IX_Theaters_MarketUrl",
                table: "Theaters",
                column: "MarketUrl");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShowTimes");

            migrationBuilder.DropTable(
                name: "Movies");

            migrationBuilder.DropTable(
                name: "Theaters");

            migrationBuilder.DropTable(
                name: "Markets");
        }
    }
}
