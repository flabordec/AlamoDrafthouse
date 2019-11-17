using Microsoft.EntityFrameworkCore.Migrations;

namespace MaguSoft.ComeAndTicket.Core.Migrations
{
    public partial class AddManyToManyShowTimeTargets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Targets_ShowTimes_ShowTimeTicketsUrl",
                table: "Targets");

            migrationBuilder.DropIndex(
                name: "IX_Targets_ShowTimeTicketsUrl",
                table: "Targets");

            migrationBuilder.DropColumn(
                name: "ShowTimeTicketsUrl",
                table: "Targets");

            migrationBuilder.CreateTable(
                name: "ShowTimeTarget",
                columns: table => new
                {
                    ShowTimeTicketsUrl = table.Column<string>(nullable: false),
                    TargetId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowTimeTarget", x => new { x.ShowTimeTicketsUrl, x.TargetId });
                    table.ForeignKey(
                        name: "FK_ShowTimeTarget_ShowTimes_ShowTimeTicketsUrl",
                        column: x => x.ShowTimeTicketsUrl,
                        principalTable: "ShowTimes",
                        principalColumn: "TicketsUrl",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShowTimeTarget_Targets_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Targets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShowTimeTarget_TargetId",
                table: "ShowTimeTarget",
                column: "TargetId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShowTimeTarget");

            migrationBuilder.AddColumn<string>(
                name: "ShowTimeTicketsUrl",
                table: "Targets",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Targets_ShowTimeTicketsUrl",
                table: "Targets",
                column: "ShowTimeTicketsUrl");

            migrationBuilder.AddForeignKey(
                name: "FK_Targets_ShowTimes_ShowTimeTicketsUrl",
                table: "Targets",
                column: "ShowTimeTicketsUrl",
                principalTable: "ShowTimes",
                principalColumn: "TicketsUrl",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
