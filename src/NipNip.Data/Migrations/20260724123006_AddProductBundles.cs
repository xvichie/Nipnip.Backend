using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductBundles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductBundles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    BundlePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBundles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductBundles_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CartBundleItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CartId = table.Column<Guid>(type: "uuid", nullable: false),
                    BundleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartBundleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartBundleItems_Carts_CartId",
                        column: x => x.CartId,
                        principalTable: "Carts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartBundleItems_ProductBundles_BundleId",
                        column: x => x.BundleId,
                        principalTable: "ProductBundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderBundleItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    BundleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    PriceAtPurchase = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderBundleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderBundleItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderBundleItems_ProductBundles_BundleId",
                        column: x => x.BundleId,
                        principalTable: "ProductBundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductBundleItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BundleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBundleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductBundleItems_ProductBundles_BundleId",
                        column: x => x.BundleId,
                        principalTable: "ProductBundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductBundleItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CartBundleItems_BundleId",
                table: "CartBundleItems",
                column: "BundleId");

            migrationBuilder.CreateIndex(
                name: "IX_CartBundleItems_CartId_BundleId",
                table: "CartBundleItems",
                columns: new[] { "CartId", "BundleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderBundleItems_BundleId",
                table: "OrderBundleItems",
                column: "BundleId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderBundleItems_OrderId",
                table: "OrderBundleItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBundleItems_BundleId_ProductId",
                table: "ProductBundleItems",
                columns: new[] { "BundleId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductBundleItems_ProductId",
                table: "ProductBundleItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBundles_StoreId_Slug",
                table: "ProductBundles",
                columns: new[] { "StoreId", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartBundleItems");

            migrationBuilder.DropTable(
                name: "OrderBundleItems");

            migrationBuilder.DropTable(
                name: "ProductBundleItems");

            migrationBuilder.DropTable(
                name: "ProductBundles");
        }
    }
}
