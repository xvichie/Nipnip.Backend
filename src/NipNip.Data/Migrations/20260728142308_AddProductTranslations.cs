using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Products.Name -> NameKa (data-preserving rename), made nullable; add NameEn/NameRu.
            migrationBuilder.RenameColumn(name: "Name", table: "Products", newName: "NameKa");
            migrationBuilder.AlterColumn<string>(name: "NameKa", table: "Products", type: "text", nullable: true, oldClrType: typeof(string), oldType: "text");
            migrationBuilder.AddColumn<string>(name: "NameEn", table: "Products", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "NameRu", table: "Products", type: "text", nullable: true);

            // Products.Description -> DescriptionKa (data-preserving rename, already nullable); add DescriptionEn/DescriptionRu.
            migrationBuilder.RenameColumn(name: "Description", table: "Products", newName: "DescriptionKa");
            migrationBuilder.AddColumn<string>(name: "DescriptionEn", table: "Products", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "DescriptionRu", table: "Products", type: "text", nullable: true);

            // ProductOptions.Name -> NameKa (data-preserving rename), made nullable; add NameEn/NameRu.
            migrationBuilder.RenameColumn(name: "Name", table: "ProductOptions", newName: "NameKa");
            migrationBuilder.AlterColumn<string>(name: "NameKa", table: "ProductOptions", type: "text", nullable: true, oldClrType: typeof(string), oldType: "text");
            migrationBuilder.AddColumn<string>(name: "NameEn", table: "ProductOptions", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "NameRu", table: "ProductOptions", type: "text", nullable: true);

            // ProductOptionValues: Value (canonical) is untouched — just add the display-only translation overlays.
            migrationBuilder.AddColumn<string>(name: "ValueKa", table: "ProductOptionValues", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ValueEn", table: "ProductOptionValues", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ValueRu", table: "ProductOptionValues", type: "text", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ValueEn", table: "ProductOptionValues");
            migrationBuilder.DropColumn(name: "ValueKa", table: "ProductOptionValues");
            migrationBuilder.DropColumn(name: "ValueRu", table: "ProductOptionValues");

            migrationBuilder.DropColumn(name: "NameEn", table: "ProductOptions");
            migrationBuilder.DropColumn(name: "NameRu", table: "ProductOptions");
            migrationBuilder.Sql(@"UPDATE ""ProductOptions"" SET ""NameKa"" = '' WHERE ""NameKa"" IS NULL");
            migrationBuilder.AlterColumn<string>(name: "NameKa", table: "ProductOptions", type: "text", nullable: false, oldClrType: typeof(string), oldType: "text", oldNullable: true);
            migrationBuilder.RenameColumn(name: "NameKa", table: "ProductOptions", newName: "Name");

            migrationBuilder.DropColumn(name: "DescriptionEn", table: "Products");
            migrationBuilder.DropColumn(name: "DescriptionRu", table: "Products");
            migrationBuilder.RenameColumn(name: "DescriptionKa", table: "Products", newName: "Description");

            migrationBuilder.DropColumn(name: "NameEn", table: "Products");
            migrationBuilder.DropColumn(name: "NameRu", table: "Products");
            migrationBuilder.Sql(@"UPDATE ""Products"" SET ""NameKa"" = '' WHERE ""NameKa"" IS NULL");
            migrationBuilder.AlterColumn<string>(name: "NameKa", table: "Products", type: "text", nullable: false, oldClrType: typeof(string), oldType: "text", oldNullable: true);
            migrationBuilder.RenameColumn(name: "NameKa", table: "Products", newName: "Name");
        }
    }
}
