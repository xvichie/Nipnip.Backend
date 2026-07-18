using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAiUsageTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AiCacheCreationInputTokens",
                table: "Orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AiCacheReadInputTokens",
                table: "Orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AiInputTokens",
                table: "Orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AiOutputTokens",
                table: "Orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TotalCacheCreationInputTokens",
                table: "Conversations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "TotalCacheReadInputTokens",
                table: "Conversations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "TotalInputTokens",
                table: "Conversations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "TotalOutputTokens",
                table: "Conversations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiCacheCreationInputTokens",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AiCacheReadInputTokens",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AiInputTokens",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AiOutputTokens",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TotalCacheCreationInputTokens",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "TotalCacheReadInputTokens",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "TotalInputTokens",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "TotalOutputTokens",
                table: "Conversations");
        }
    }
}
