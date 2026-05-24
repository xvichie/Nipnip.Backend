using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformFees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CreatorEarnings",
                table: "Conversions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CreatorFeeAmount",
                table: "Conversions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MerchantFeeAmount",
                table: "Conversions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatorEarnings",
                table: "Conversions");

            migrationBuilder.DropColumn(
                name: "CreatorFeeAmount",
                table: "Conversions");

            migrationBuilder.DropColumn(
                name: "MerchantFeeAmount",
                table: "Conversions");
        }
    }
}
