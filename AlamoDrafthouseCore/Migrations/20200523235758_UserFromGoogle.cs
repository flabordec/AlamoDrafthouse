using Microsoft.EntityFrameworkCore.Migrations;

namespace MaguSoft.ComeAndTicket.Core.Migrations
{
    public partial class UserFromGoogle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "User_UserName_Unique",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "Users",
                newName: "EMail");

            migrationBuilder.AddUniqueConstraint(
                name: "User_EMail_Unique",
                table: "Users",
                column: "EMail");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "User_EMail_Unique",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.RenameColumn(
                name: "EMail",
                table: "Users",
                newName: "UserName");

            migrationBuilder.AddUniqueConstraint(
                name: "User_UserName_Unique",
                table: "Users",
                column: "UserName");
        }
    }
}
