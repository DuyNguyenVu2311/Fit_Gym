using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitGym.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOnlineRenewalOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MaNhanVien",
                table: "THANH_TOAN",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "MaNhanVien",
                table: "DANG_KY_GOI_TAP",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "MaDangKyGoc",
                table: "DANG_KY_GOI_TAP",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DANG_KY_GOI_TAP_MaDangKyGoc",
                table: "DANG_KY_GOI_TAP",
                column: "MaDangKyGoc");

            migrationBuilder.AddForeignKey(
                name: "FK_DANG_KY_GOI_TAP_DANG_KY_GOI_TAP_MaDangKyGoc",
                table: "DANG_KY_GOI_TAP",
                column: "MaDangKyGoc",
                principalTable: "DANG_KY_GOI_TAP",
                principalColumn: "MaDangKy",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DANG_KY_GOI_TAP_DANG_KY_GOI_TAP_MaDangKyGoc",
                table: "DANG_KY_GOI_TAP");

            migrationBuilder.DropIndex(
                name: "IX_DANG_KY_GOI_TAP_MaDangKyGoc",
                table: "DANG_KY_GOI_TAP");

            migrationBuilder.DropColumn(
                name: "MaDangKyGoc",
                table: "DANG_KY_GOI_TAP");

            migrationBuilder.AlterColumn<int>(
                name: "MaNhanVien",
                table: "THANH_TOAN",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MaNhanVien",
                table: "DANG_KY_GOI_TAP",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
