using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebPortal.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedDuyuruColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnnouncementFiles_Duyurular_AnnouncementId",
                table: "AnnouncementFiles");

            migrationBuilder.DropColumn(
                name: "Icerik",
                table: "Duyurular");

            migrationBuilder.DropColumn(
                name: "Ikon",
                table: "Duyurular");

            migrationBuilder.DropColumn(
                name: "Kategori",
                table: "Duyurular");

            migrationBuilder.RenameColumn(
                name: "AnnouncementId",
                table: "AnnouncementFiles",
                newName: "DuyuruId");

            migrationBuilder.RenameIndex(
                name: "IX_AnnouncementFiles_AnnouncementId",
                table: "AnnouncementFiles",
                newName: "IX_AnnouncementFiles_DuyuruId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnnouncementFiles_Duyurular_DuyuruId",
                table: "AnnouncementFiles",
                column: "DuyuruId",
                principalTable: "Duyurular",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnnouncementFiles_Duyurular_DuyuruId",
                table: "AnnouncementFiles");

            migrationBuilder.RenameColumn(
                name: "DuyuruId",
                table: "AnnouncementFiles",
                newName: "AnnouncementId");

            migrationBuilder.RenameIndex(
                name: "IX_AnnouncementFiles_DuyuruId",
                table: "AnnouncementFiles",
                newName: "IX_AnnouncementFiles_AnnouncementId");

            migrationBuilder.AddColumn<string>(
                name: "Icerik",
                table: "Duyurular",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Ikon",
                table: "Duyurular",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Kategori",
                table: "Duyurular",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_AnnouncementFiles_Duyurular_AnnouncementId",
                table: "AnnouncementFiles",
                column: "AnnouncementId",
                principalTable: "Duyurular",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
