using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialPlatforms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FacebookFollowers",
                table: "Creators",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacebookHandle",
                table: "Creators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LinkedinFollowers",
                table: "Creators",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedinHandle",
                table: "Creators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "XFollowers",
                table: "Creators",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "XHandle",
                table: "Creators",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YoutubeFollowers",
                table: "Creators",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YoutubeHandle",
                table: "Creators",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FacebookFollowers",
                table: "Creators");

            migrationBuilder.DropColumn(
                name: "FacebookHandle",
                table: "Creators");

            migrationBuilder.DropColumn(
                name: "LinkedinFollowers",
                table: "Creators");

            migrationBuilder.DropColumn(
                name: "LinkedinHandle",
                table: "Creators");

            migrationBuilder.DropColumn(
                name: "XFollowers",
                table: "Creators");

            migrationBuilder.DropColumn(
                name: "XHandle",
                table: "Creators");

            migrationBuilder.DropColumn(
                name: "YoutubeFollowers",
                table: "Creators");

            migrationBuilder.DropColumn(
                name: "YoutubeHandle",
                table: "Creators");
        }
    }
}
