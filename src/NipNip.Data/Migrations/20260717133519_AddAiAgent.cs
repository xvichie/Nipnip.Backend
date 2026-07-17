using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAiAgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AiAgentEnabled",
                table: "Stores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AiAgentInstructions",
                table: "Stores",
                type: "text",
                nullable: true);

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

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalUserId = table.Column<string>(type: "text", nullable: false),
                    CustomerDisplayName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastMessageAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conversations_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConversationMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExternalMessageId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationMessages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_ConversationId",
                table: "ConversationMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_ExternalMessageId",
                table: "ConversationMessages",
                column: "ExternalMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_StoreId_ExternalUserId",
                table: "Conversations",
                columns: new[] { "StoreId", "ExternalUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversationMessages");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropColumn(
                name: "AiAgentEnabled",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "AiAgentInstructions",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "AiAgentReturnPolicy",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "AiAgentShippingPolicy",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Orders");
        }
    }
}
