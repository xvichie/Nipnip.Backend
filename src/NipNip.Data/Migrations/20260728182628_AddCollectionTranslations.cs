using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NipNip.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Collections.Name -> NameKa (data-preserving rename), made nullable; add NameEn/NameRu.
            migrationBuilder.RenameColumn(name: "Name", table: "Collections", newName: "NameKa");
            migrationBuilder.AlterColumn<string>(name: "NameKa", table: "Collections", type: "text", nullable: true, oldClrType: typeof(string), oldType: "text");
            migrationBuilder.AddColumn<string>(name: "NameEn", table: "Collections", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "NameRu", table: "Collections", type: "text", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "NameEn", table: "Collections");
            migrationBuilder.DropColumn(name: "NameRu", table: "Collections");
            migrationBuilder.Sql(@"UPDATE ""Collections"" SET ""NameKa"" = '' WHERE ""NameKa"" IS NULL");
            migrationBuilder.AlterColumn<string>(name: "NameKa", table: "Collections", type: "text", nullable: false, oldClrType: typeof(string), oldType: "text", oldNullable: true);
            migrationBuilder.RenameColumn(name: "NameKa", table: "Collections", newName: "Name");
        }
    }
}
