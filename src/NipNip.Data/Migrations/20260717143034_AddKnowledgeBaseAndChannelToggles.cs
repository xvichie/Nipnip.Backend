using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeBaseAndChannelToggles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnowledgeBaseSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Embedding = table.Column<float[]>(type: "real[]", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeBaseSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeBaseSections_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseSections_StoreId",
                table: "KnowledgeBaseSections",
                column: "StoreId");

            // Carry the old fixed-field policy text into the new sections system before
            // dropping the columns — embeddings are backfilled lazily on next settings
            // page view (see KnowledgeBaseService.ListForOwnStoreAsync), not here.
            migrationBuilder.Sql("""
                INSERT INTO "KnowledgeBaseSections" ("Id", "StoreId", "Title", "Content", "Embedding", "CreatedAt", "UpdatedAt")
                SELECT gen_random_uuid(), "Id", 'Shipping Policy', "AiAgentShippingPolicy", NULL, now(), now()
                FROM "Stores"
                WHERE "AiAgentShippingPolicy" IS NOT NULL AND "AiAgentShippingPolicy" != '';
                """);

            migrationBuilder.Sql("""
                INSERT INTO "KnowledgeBaseSections" ("Id", "StoreId", "Title", "Content", "Embedding", "CreatedAt", "UpdatedAt")
                SELECT gen_random_uuid(), "Id", 'Return Policy', "AiAgentReturnPolicy", NULL, now(), now()
                FROM "Stores"
                WHERE "AiAgentReturnPolicy" IS NOT NULL AND "AiAgentReturnPolicy" != '';
                """);

            migrationBuilder.DropColumn(
                name: "AiAgentReturnPolicy",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "AiAgentShippingPolicy",
                table: "Stores");

            // NOTE: the scaffolded migration mapped this rename to AiAgentEnabledInstagram
            // by mistake — the pre-existing column held Facebook enablement, so it must
            // become AiAgentEnabledFacebook or every store with the agent on today would
            // silently flip to "Instagram enabled, Facebook disabled" on deploy.
            migrationBuilder.RenameColumn(
                name: "AiAgentEnabled",
                table: "Stores",
                newName: "AiAgentEnabledFacebook");

            migrationBuilder.AddColumn<bool>(
                name: "AiAgentEnabledInstagram",
                table: "Stores",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowledgeBaseSections");

            migrationBuilder.DropColumn(
                name: "AiAgentEnabledInstagram",
                table: "Stores");

            migrationBuilder.RenameColumn(
                name: "AiAgentEnabledFacebook",
                table: "Stores",
                newName: "AiAgentEnabled");

            migrationBuilder.AddColumn<string>(
                name: "AiAgentReturnPolicy",
                table: "Stores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiAgentShippingPolicy",
                table: "Stores",
                type: "text",
                nullable: true);
        }
    }
}
