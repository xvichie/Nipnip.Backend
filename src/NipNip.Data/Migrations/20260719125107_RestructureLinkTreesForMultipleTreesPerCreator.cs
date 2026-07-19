using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class RestructureLinkTreesForMultipleTreesPerCreator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LinkTreeItems_Creators_CreatorId",
                table: "LinkTreeItems");

            migrationBuilder.RenameColumn(
                name: "CreatorId",
                table: "LinkTreeItems",
                newName: "LinkTreeId");

            migrationBuilder.RenameIndex(
                name: "IX_LinkTreeItems_CreatorId_MerchantId",
                table: "LinkTreeItems",
                newName: "IX_LinkTreeItems_LinkTreeId_MerchantId");

            migrationBuilder.CreateTable(
                name: "LinkTrees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkTrees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LinkTrees_Creators_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Creators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LinkTrees_CreatorId",
                table: "LinkTrees",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_LinkTrees_Slug",
                table: "LinkTrees",
                column: "Slug",
                unique: true);

            // Data backfill: the RenameColumn above only renamed CreatorId -> LinkTreeId — the
            // existing rows still hold the *creator's* id, not a real LinkTree id. Create one
            // default tree per creator that already has items (using their own slug), then
            // repoint LinkTreeItems.LinkTreeId at that tree before the FK below is added — the FK
            // would otherwise fail to validate against the stale creator-id values.
            migrationBuilder.Sql("""
                INSERT INTO "LinkTrees" ("Id", "CreatorId", "Name", "Slug", "IsDefault", "Position", "CreatedAt")
                SELECT gen_random_uuid(), c."Id", 'My Links', c."Slug", true, 0, now()
                FROM "Creators" c
                WHERE EXISTS (SELECT 1 FROM "LinkTreeItems" li WHERE li."LinkTreeId" = c."Id");
                """);

            migrationBuilder.Sql("""
                UPDATE "LinkTreeItems" li
                SET "LinkTreeId" = lt."Id"
                FROM "LinkTrees" lt
                WHERE lt."CreatorId" = li."LinkTreeId" AND lt."IsDefault" = true;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_LinkTreeItems_LinkTrees_LinkTreeId",
                table: "LinkTreeItems",
                column: "LinkTreeId",
                principalTable: "LinkTrees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LinkTreeItems_LinkTrees_LinkTreeId",
                table: "LinkTreeItems");

            migrationBuilder.DropTable(
                name: "LinkTrees");

            migrationBuilder.RenameColumn(
                name: "LinkTreeId",
                table: "LinkTreeItems",
                newName: "CreatorId");

            migrationBuilder.RenameIndex(
                name: "IX_LinkTreeItems_LinkTreeId_MerchantId",
                table: "LinkTreeItems",
                newName: "IX_LinkTreeItems_CreatorId_MerchantId");

            migrationBuilder.AddForeignKey(
                name: "FK_LinkTreeItems_Creators_CreatorId",
                table: "LinkTreeItems",
                column: "CreatorId",
                principalTable: "Creators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
