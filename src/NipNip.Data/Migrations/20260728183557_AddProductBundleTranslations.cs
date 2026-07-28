using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductBundleTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ProductBundles.Name -> NameKa (data-preserving rename), made nullable; add NameEn/NameRu.
            migrationBuilder.RenameColumn(name: "Name", table: "ProductBundles", newName: "NameKa");
            migrationBuilder.AlterColumn<string>(name: "NameKa", table: "ProductBundles", type: "text", nullable: true, oldClrType: typeof(string), oldType: "text");
            migrationBuilder.AddColumn<string>(name: "NameEn", table: "ProductBundles", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "NameRu", table: "ProductBundles", type: "text", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "NameEn", table: "ProductBundles");
            migrationBuilder.DropColumn(name: "NameRu", table: "ProductBundles");
            migrationBuilder.Sql(@"UPDATE ""ProductBundles"" SET ""NameKa"" = '' WHERE ""NameKa"" IS NULL");
            migrationBuilder.AlterColumn<string>(name: "NameKa", table: "ProductBundles", type: "text", nullable: false, oldClrType: typeof(string), oldType: "text", oldNullable: true);
            migrationBuilder.RenameColumn(name: "NameKa", table: "ProductBundles", newName: "Name");
        }
    }
}
