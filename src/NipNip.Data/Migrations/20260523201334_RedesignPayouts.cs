using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class RedesignPayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PeriodEnd",
                table: "Payouts");

            migrationBuilder.RenameColumn(
                name: "PeriodStart",
                table: "Payouts",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "Payouts",
                newName: "RequestedAmount");

            migrationBuilder.AddColumn<decimal>(
                name: "AmountSent",
                table: "Payouts",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Payouts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountSent",
                table: "Payouts");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Payouts");

            migrationBuilder.RenameColumn(
                name: "RequestedAmount",
                table: "Payouts",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Payouts",
                newName: "PeriodStart");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PeriodEnd",
                table: "Payouts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
