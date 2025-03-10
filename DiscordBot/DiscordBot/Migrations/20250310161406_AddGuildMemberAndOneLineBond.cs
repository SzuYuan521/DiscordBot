using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class AddGuildMemberAndOneLineBond : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildMembers",
                columns: table => new
                {
                    DiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false),
                    DiscordName = table.Column<string>(type: "text", nullable: false),
                    CharacterClass = table.Column<int>(type: "integer", nullable: true),
                    JoinDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildMembers", x => x.DiscordId);
                });

            migrationBuilder.CreateTable(
                name: "OneLineBonds",
                columns: table => new
                {
                    BondId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    PartnerId = table.Column<int>(type: "integer", nullable: true),
                    PartnerName = table.Column<string>(type: "text", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneLineBonds", x => x.BondId);
                    table.ForeignKey(
                        name: "FK_OneLineBonds_GuildMembers_DiscordId",
                        column: x => x.DiscordId,
                        principalTable: "GuildMembers",
                        principalColumn: "DiscordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OneLineBonds_DiscordId",
                table: "OneLineBonds",
                column: "DiscordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OneLineBonds");

            migrationBuilder.DropTable(
                name: "GuildMembers");
        }
    }
}
