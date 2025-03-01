using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class FixMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonitoredMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitoredMessages", x => x.Id);
                    table.UniqueConstraint("AK_MonitoredMessages_MessageId", x => x.MessageId);
                });

            migrationBuilder.CreateTable(
                name: "RoleMagicPacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DiscordChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    MonitoredMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Emoji = table.Column<string>(type: "text", nullable: true),
                    DiscordRoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DiscordRoleType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleMagicPacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleMagicPacts_DiscordChannels_DiscordChannelId",
                        column: x => x.DiscordChannelId,
                        principalTable: "DiscordChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleMagicPacts_DiscordRoles_DiscordRoleId",
                        column: x => x.DiscordRoleId,
                        principalTable: "DiscordRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleMagicPacts_MonitoredMessages_MonitoredMessageId",
                        column: x => x.MonitoredMessageId,
                        principalTable: "MonitoredMessages",
                        principalColumn: "MessageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonitoredMessages_MessageId",
                table: "MonitoredMessages",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleMagicPacts_DiscordChannelId",
                table: "RoleMagicPacts",
                column: "DiscordChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleMagicPacts_DiscordRoleId",
                table: "RoleMagicPacts",
                column: "DiscordRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleMagicPacts_MonitoredMessageId",
                table: "RoleMagicPacts",
                column: "MonitoredMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleMagicPacts");

            migrationBuilder.DropTable(
                name: "MonitoredMessages");
        }
    }
}
