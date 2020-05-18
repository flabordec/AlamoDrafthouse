using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace MaguSoft.ComeAndTicket.Core.Migrations
{
    public partial class Users : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShowTimeTarget");

            migrationBuilder.DropTable(
                name: "Targets");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserName = table.Column<string>(nullable: false),
                    PasswordHash = table.Column<string>(nullable: false),
                    MovieTitlesToWatch = table.Column<string>(nullable: true),
                    DeviceNicknames = table.Column<string>(nullable: true),
                    PushbulletApiKey = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.UniqueConstraint("User_UserName_Unique", x => x.UserName);
                });

            migrationBuilder.CreateTable(
                name: "ShowTimeNotification",
                columns: table => new
                {
                    ShowTimeTicketsUrl = table.Column<string>(nullable: false),
                    UserId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowTimeNotification", x => new { x.ShowTimeTicketsUrl, x.UserId });
                    table.ForeignKey(
                        name: "FK_ShowTimeNotification_ShowTimes_ShowTimeTicketsUrl",
                        column: x => x.ShowTimeTicketsUrl,
                        principalTable: "ShowTimes",
                        principalColumn: "TicketsUrl",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShowTimeNotification_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShowTimeNotification_UserId",
                table: "ShowTimeNotification",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShowTimeNotification");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.CreateTable(
                name: "Targets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Nickname = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Targets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShowTimeTarget",
                columns: table => new
                {
                    ShowTimeTicketsUrl = table.Column<string>(type: "text", nullable: false),
                    TargetId = table.Column<string>(type: "text", nullable: false)
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
    }
}
