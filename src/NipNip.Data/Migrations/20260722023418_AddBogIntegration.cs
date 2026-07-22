using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBogIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BogAccessTokenEncrypted",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BogClientId",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BogClientSecretEncrypted",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BogConnectedAt",
                table: "Stores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BogTokenExpiresAt",
                table: "Stores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BogMerchantData",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BogPreOrderId",
                table: "Orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BogAccessTokenEncrypted",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "BogClientId",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "BogClientSecretEncrypted",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "BogConnectedAt",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "BogTokenExpiresAt",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "BogMerchantData",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BogPreOrderId",
                table: "Orders");
        }
    }
}
