using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDiscordRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscordRoleType",
                table: "RoleMagicPacts");

            migrationBuilder.AddColumn<int>(
                name: "DiscordRoleType",
                table: "DiscordRoles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscordRoleType",
                table: "DiscordRoles");

            migrationBuilder.AddColumn<int>(
                name: "DiscordRoleType",
                table: "RoleMagicPacts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
