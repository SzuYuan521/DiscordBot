using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiscordBot.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMemberStatisticsIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MemberStatistics",
                table: "MemberStatistics");

            migrationBuilder.DropIndex(
                name: "IX_MemberStatistics_DiscordId",
                table: "MemberStatistics");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "MemberStatistics");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MemberStatistics",
                table: "MemberStatistics",
                column: "DiscordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MemberStatistics",
                table: "MemberStatistics");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "MemberStatistics",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MemberStatistics",
                table: "MemberStatistics",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_MemberStatistics_DiscordId",
                table: "MemberStatistics",
                column: "DiscordId",
                unique: true);
        }
    }
}
