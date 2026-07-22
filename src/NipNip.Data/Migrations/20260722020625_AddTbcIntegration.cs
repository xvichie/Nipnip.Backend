using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTbcIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TbcAccessTokenEncrypted",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TbcClientId",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TbcClientSecretEncrypted",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TbcConnectedAt",
                table: "Stores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TbcTokenExpiresAt",
                table: "Stores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TbcMerchantData",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TbcPaymentId",
                table: "Orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TbcAccessTokenEncrypted",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "TbcClientId",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "TbcClientSecretEncrypted",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "TbcConnectedAt",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "TbcTokenExpiresAt",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "TbcMerchantData",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TbcPaymentId",
                table: "Orders");
        }
    }
}
