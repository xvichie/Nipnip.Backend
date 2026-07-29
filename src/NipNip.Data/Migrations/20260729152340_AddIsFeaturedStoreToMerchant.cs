using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsFeaturedStoreToMerchant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFeaturedStore",
                table: "Merchants",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFeaturedStore",
                table: "Merchants");
        }
    }
}
