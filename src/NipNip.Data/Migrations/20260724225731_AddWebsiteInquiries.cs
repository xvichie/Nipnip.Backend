using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWebsiteInquiries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WebsiteInquiries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Message = table.Column<string>(type: "text", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebsiteInquiries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebsiteInquiries_CreatedAt",
                table: "WebsiteInquiries",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebsiteInquiries");
        }
    }
}
