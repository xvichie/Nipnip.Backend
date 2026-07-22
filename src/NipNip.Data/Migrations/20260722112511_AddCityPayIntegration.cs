using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCityPayIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CityPayAccessTokenEncrypted",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CityPayConnectedAt",
                table: "Stores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CityPayCustomerId",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CityPayMerchantData",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CityPayOrderId",
                table: "Orders",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CityPayAccessTokenEncrypted",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "CityPayConnectedAt",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "CityPayCustomerId",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "CityPayMerchantData",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CityPayOrderId",
                table: "Orders");
        }
    }
}
