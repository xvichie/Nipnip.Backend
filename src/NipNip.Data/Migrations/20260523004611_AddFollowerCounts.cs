using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFollowerCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InstagramFollowers",
                table: "Creators",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TiktokFollowers",
                table: "Creators",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstagramFollowers",
                table: "Creators");

            migrationBuilder.DropColumn(
                name: "TiktokFollowers",
                table: "Creators");
        }
    }
}
