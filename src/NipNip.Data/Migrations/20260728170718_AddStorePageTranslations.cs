using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStorePageTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // StorePages.Title -> TitleKa (data-preserving rename), made nullable; add TitleEn/TitleRu.
            migrationBuilder.RenameColumn(name: "Title", table: "StorePages", newName: "TitleKa");
            migrationBuilder.AlterColumn<string>(name: "TitleKa", table: "StorePages", type: "text", nullable: true, oldClrType: typeof(string), oldType: "text");
            migrationBuilder.AddColumn<string>(name: "TitleEn", table: "StorePages", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "TitleRu", table: "StorePages", type: "text", nullable: true);

            // StorePages.Content -> ContentKa (data-preserving rename), made nullable; add ContentEn/ContentRu.
            migrationBuilder.RenameColumn(name: "Content", table: "StorePages", newName: "ContentKa");
            migrationBuilder.AlterColumn<string>(name: "ContentKa", table: "StorePages", type: "text", nullable: true, oldClrType: typeof(string), oldType: "text");
            migrationBuilder.AddColumn<string>(name: "ContentEn", table: "StorePages", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ContentRu", table: "StorePages", type: "text", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ContentEn", table: "StorePages");
            migrationBuilder.DropColumn(name: "ContentRu", table: "StorePages");
            migrationBuilder.Sql(@"UPDATE ""StorePages"" SET ""ContentKa"" = '' WHERE ""ContentKa"" IS NULL");
            migrationBuilder.AlterColumn<string>(name: "ContentKa", table: "StorePages", type: "text", nullable: false, oldClrType: typeof(string), oldType: "text", oldNullable: true);
            migrationBuilder.RenameColumn(name: "ContentKa", table: "StorePages", newName: "Content");

            migrationBuilder.DropColumn(name: "TitleEn", table: "StorePages");
            migrationBuilder.DropColumn(name: "TitleRu", table: "StorePages");
            migrationBuilder.Sql(@"UPDATE ""StorePages"" SET ""TitleKa"" = '' WHERE ""TitleKa"" IS NULL");
            migrationBuilder.AlterColumn<string>(name: "TitleKa", table: "StorePages", type: "text", nullable: false, oldClrType: typeof(string), oldType: "text", oldNullable: true);
            migrationBuilder.RenameColumn(name: "TitleKa", table: "StorePages", newName: "Title");
        }
    }
}
