using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTikTokIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TikTokAccessTokenEncrypted",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TikTokConnectedAt",
                table: "Stores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TikTokDisplayName",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TikTokOpenId",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TikTokRefreshTokenEncrypted",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TikTokTokenExpiresAt",
                table: "Stores",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TikTokAccessTokenEncrypted",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "TikTokConnectedAt",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "TikTokDisplayName",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "TikTokOpenId",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "TikTokRefreshTokenEncrypted",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "TikTokTokenExpiresAt",
                table: "Stores");
        }
    }
}
