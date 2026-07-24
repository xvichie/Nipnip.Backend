using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWebsiteInquiryStoreNameAndClerkUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClerkUserId",
                table: "WebsiteInquiries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreName",
                table: "WebsiteInquiries",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebsiteInquiries_ClerkUserId",
                table: "WebsiteInquiries",
                column: "ClerkUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WebsiteInquiries_ClerkUserId",
                table: "WebsiteInquiries");

            migrationBuilder.DropColumn(
                name: "ClerkUserId",
                table: "WebsiteInquiries");

            migrationBuilder.DropColumn(
                name: "StoreName",
                table: "WebsiteInquiries");
        }
    }
}
