using Microsoft.EntityFrameworkCore.Migrations;

namespace MaguSoft.ComeAndTicket.Core.Migrations
{
    public partial class AddConfigurationAndShowTimesPushedTo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Configuration",
                columns: table => new
                {
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuration", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "Targets",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Nickname = table.Column<string>(nullable: true),
                    ShowTimeTicketsUrl = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Targets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Targets_ShowTimes_ShowTimeTicketsUrl",
                        column: x => x.ShowTimeTicketsUrl,
                        principalTable: "ShowTimes",
                        principalColumn: "TicketsUrl",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Configuration_Name",
                table: "Configuration",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Targets_ShowTimeTicketsUrl",
                table: "Targets",
                column: "ShowTimeTicketsUrl");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configuration");

            migrationBuilder.DropTable(
                name: "Targets");
        }
    }
}
