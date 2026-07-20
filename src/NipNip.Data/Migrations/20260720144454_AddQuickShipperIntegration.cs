using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuickShipperIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PickupAddress",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupContactName",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PickupLatitude",
                table: "Stores",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PickupLongitude",
                table: "Stores",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupPhone",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuickShipperAccessTokenEncrypted",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "QuickShipperConnectedAt",
                table: "Stores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuickShipperPasswordEncrypted",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "QuickShipperTokenExpiresAt",
                table: "Stores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuickShipperUsernameEncrypted",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuickShipperDeliveryFee",
                table: "Orders",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuickShipperOrderId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuickShipperStatus",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuickShipperTrackingUrl",
                table: "Orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PickupAddress",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "PickupContactName",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "PickupLatitude",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "PickupLongitude",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "PickupPhone",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "QuickShipperAccessTokenEncrypted",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "QuickShipperConnectedAt",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "QuickShipperPasswordEncrypted",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "QuickShipperTokenExpiresAt",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "QuickShipperUsernameEncrypted",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "QuickShipperDeliveryFee",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "QuickShipperOrderId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "QuickShipperStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "QuickShipperTrackingUrl",
                table: "Orders");
        }
    }
}
