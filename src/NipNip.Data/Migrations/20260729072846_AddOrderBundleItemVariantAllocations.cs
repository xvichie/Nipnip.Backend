using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderBundleItemVariantAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderBundleItemVariantAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderBundleItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderBundleItemVariantAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderBundleItemVariantAllocations_OrderBundleItems_OrderBun~",
                        column: x => x.OrderBundleItemId,
                        principalTable: "OrderBundleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderBundleItemVariantAllocations_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderBundleItemVariantAllocations_OrderBundleItemId",
                table: "OrderBundleItemVariantAllocations",
                column: "OrderBundleItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderBundleItemVariantAllocations_VariantId",
                table: "OrderBundleItemVariantAllocations",
                column: "VariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderBundleItemVariantAllocations");
        }
    }
}
