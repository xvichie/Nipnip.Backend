using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFlittPaymentIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FlittConnectedAt",
                table: "Stores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlittMerchantId",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlittSecretKeyEncrypted",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FlittPaymentId",
                table: "Orders",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlittConnectedAt",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "FlittMerchantId",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "FlittSecretKeyEncrypted",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "FlittPaymentId",
                table: "Orders");
        }
    }
}
