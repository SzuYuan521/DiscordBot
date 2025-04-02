using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class CreateGuildTeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GroupName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildTeams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamGroupId = table.Column<int>(type: "integer", nullable: false),
                    TeamName = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildTeams_TeamGroups_TeamGroupId",
                        column: x => x.TeamGroupId,
                        principalTable: "TeamGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildTeamMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildTeamId = table.Column<int>(type: "integer", nullable: false),
                    DiscordMemberId = table.Column<long>(type: "bigint", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildTeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildTeamMembers_GuildTeams_GuildTeamId",
                        column: x => x.GuildTeamId,
                        principalTable: "GuildTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuildTeamMembers_GuildTeamId",
                table: "GuildTeamMembers",
                column: "GuildTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildTeams_TeamGroupId",
                table: "GuildTeams",
                column: "TeamGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildTeamMembers");

            migrationBuilder.DropTable(
                name: "GuildTeams");

            migrationBuilder.DropTable(
                name: "TeamGroups");
        }
    }
}
