using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreFacebookConnection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FacebookConnectedAt",
                table: "Stores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacebookPageAccessTokenEncrypted",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacebookPageId",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacebookPageName",
                table: "Stores",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FacebookConnectedAt",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "FacebookPageAccessTokenEncrypted",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "FacebookPageId",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "FacebookPageName",
                table: "Stores");
        }
    }
}
