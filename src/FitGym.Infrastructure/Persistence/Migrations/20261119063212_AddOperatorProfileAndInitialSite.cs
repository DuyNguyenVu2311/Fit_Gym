using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitGym.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperatorProfileAndInitialSite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DON_VI_SU_DUNG",
                columns: table => new
                {
                    MaDonVi = table.Column<int>(type: "int", nullable: false),
                    TenDonVi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Logo = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    LogoContentType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DON_VI_SU_DUNG", x => x.MaDonVi);
                    table.CheckConstraint("CK_DON_VI_SINGLE_INSTALL", "[MaDonVi] = 1");
                });

            migrationBuilder.CreateTable(
                name: "CO_SO",
                columns: table => new
                {
                    MaCoSo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDonVi = table.Column<int>(type: "int", nullable: false),
                    TenCoSo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CO_SO", x => x.MaCoSo);
                    table.ForeignKey(
                        name: "FK_CO_SO_DON_VI_SU_DUNG_MaDonVi",
                        column: x => x.MaDonVi,
                        principalTable: "DON_VI_SU_DUNG",
                        principalColumn: "MaDonVi",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CO_SO_MaDonVi_TenCoSo",
                table: "CO_SO",
                columns: new[] { "MaDonVi", "TenCoSo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CO_SO");

            migrationBuilder.DropTable(
                name: "DON_VI_SU_DUNG");
        }
    }
}
