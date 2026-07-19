using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantAccessRequestWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Renamed (not dropped/recreated) to preserve existing rows — the table was previously
            // a plain "already approved" allowlist, now it's a full request/response workflow.
            migrationBuilder.RenameTable(
                name: "MerchantApprovedCreators",
                newName: "MerchantAccessRequests");

            migrationBuilder.RenameIndex(
                name: "IX_MerchantApprovedCreators_CreatorId",
                table: "MerchantAccessRequests",
                newName: "IX_MerchantAccessRequests_CreatorId");

            migrationBuilder.RenameIndex(
                name: "IX_MerchantApprovedCreators_MerchantId_CreatorId",
                table: "MerchantAccessRequests",
                newName: "IX_MerchantAccessRequests_MerchantId_CreatorId");

            // Every pre-existing row was a merchant-direct-add under the old model, i.e. already
            // approved — default them to Approved (1) rather than Pending (0).
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "MerchantAccessRequests",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RespondedAt",
                table: "MerchantAccessRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "MerchantAccessRequests" SET "RespondedAt" = "CreatedAt" WHERE "RespondedAt" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RespondedAt",
                table: "MerchantAccessRequests");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "MerchantAccessRequests");

            migrationBuilder.RenameIndex(
                name: "IX_MerchantAccessRequests_MerchantId_CreatorId",
                table: "MerchantAccessRequests",
                newName: "IX_MerchantApprovedCreators_MerchantId_CreatorId");

            migrationBuilder.RenameIndex(
                name: "IX_MerchantAccessRequests_CreatorId",
                table: "MerchantAccessRequests",
                newName: "IX_MerchantApprovedCreators_CreatorId");

            migrationBuilder.RenameTable(
                name: "MerchantAccessRequests",
                newName: "MerchantApprovedCreators");
        }
    }
}
