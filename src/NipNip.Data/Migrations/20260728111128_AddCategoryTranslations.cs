using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve existing category names (entered in Georgian, the platform's original
            // language) instead of the scaffolded drop+add, which would have silently deleted
            // every category's name.
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Categories",
                newName: "NameKa");

            migrationBuilder.AlterColumn<string>(
                name: "NameKa",
                table: "Categories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "Categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameRu",
                table: "Categories",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "NameRu",
                table: "Categories");

            // Any category left with only NameEn/NameRu filled (no NameKa) loses its name on
            // rollback — an acceptable, documented limitation of reversing this migration.
            migrationBuilder.Sql(@"UPDATE ""Categories"" SET ""NameKa"" = '' WHERE ""NameKa"" IS NULL");

            migrationBuilder.AlterColumn<string>(
                name: "NameKa",
                table: "Categories",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "NameKa",
                table: "Categories",
                newName: "Name");
        }
    }
}
