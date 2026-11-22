using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitGym.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BAI_TAP",
                columns: table => new
                {
                    MaBaiTap = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenBaiTap = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NhomCoChinh = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LinkVideo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HoTroAI = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BAI_TAP", x => x.MaBaiTap);
                });

            migrationBuilder.CreateTable(
                name: "GOI_TAP",
                columns: table => new
                {
                    MaGoiTap = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenGoi = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ThoiHanThang = table.Column<int>(type: "int", nullable: false),
                    GiaTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GOI_TAP", x => x.MaGoiTap);
                    table.CheckConstraint("CK_GOI_TAP_Gia", "[GiaTien] >= 0");
                    table.CheckConstraint("CK_GOI_TAP_ThoiHan", "[ThoiHanThang] > 0");
                });

            migrationBuilder.CreateTable(
                name: "HANG_HOA",
                columns: table => new
                {
                    MaHangHoa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenHang = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DonViTinh = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    GiaBan = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HANG_HOA", x => x.MaHangHoa);
                    table.CheckConstraint("CK_HANG_HOA_GiaBan", "[GiaBan] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "THIET_BI",
                columns: table => new
                {
                    MaThietBi = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenThietBi = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PhanLoai = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NgayMua = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_THIET_BI", x => x.MaThietBi);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HOI_VIEN",
                columns: table => new
                {
                    MaHoiVien = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: true),
                    HoTen = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NgaySinh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GioiTinh = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SoDienThoai = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NgayThamGia = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HOI_VIEN", x => x.MaHoiVien);
                    table.ForeignKey(
                        name: "FK_HOI_VIEN_AspNetUsers_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NHAN_VIEN",
                columns: table => new
                {
                    MaNhanVien = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ChucVu = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NgayVaoLam = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NHAN_VIEN", x => x.MaNhanVien);
                    table.ForeignKey(
                        name: "FK_NHAN_VIEN_AspNetUsers_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DONG_Y_SINH_TRAC_HOC",
                columns: table => new
                {
                    MaDongY = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHoiVien = table.Column<int>(type: "int", nullable: false),
                    NgayDongY = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayRutLai = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhienBanNoiDung = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DONG_Y_SINH_TRAC_HOC", x => x.MaDongY);
                    table.CheckConstraint("CK_DONG_Y_NgayRutLai", "[NgayRutLai] IS NULL OR [NgayRutLai] >= [NgayDongY]");
                    table.ForeignKey(
                        name: "FK_DONG_Y_SINH_TRAC_HOC_HOI_VIEN_MaHoiVien",
                        column: x => x.MaHoiVien,
                        principalTable: "HOI_VIEN",
                        principalColumn: "MaHoiVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DU_LIEU_KHUON_MAT",
                columns: table => new
                {
                    MaDuLieuMat = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHoiVien = table.Column<int>(type: "int", nullable: false),
                    VectorKhuonMat = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PhienBanMoHinh = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DangHoatDong = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DU_LIEU_KHUON_MAT", x => x.MaDuLieuMat);
                    table.ForeignKey(
                        name: "FK_DU_LIEU_KHUON_MAT_HOI_VIEN_MaHoiVien",
                        column: x => x.MaHoiVien,
                        principalTable: "HOI_VIEN",
                        principalColumn: "MaHoiVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HO_SO_TAP_LUYEN",
                columns: table => new
                {
                    MaHoSo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHoiVien = table.Column<int>(type: "int", nullable: false),
                    MucTieu = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TrinhDo = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SoNgayTuan = table.Column<int>(type: "int", nullable: false),
                    SoPhutBuoi = table.Column<int>(type: "int", nullable: false),
                    DieuKienTap = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HO_SO_TAP_LUYEN", x => x.MaHoSo);
                    table.CheckConstraint("CK_HO_SO_SoNgayTuan", "[SoNgayTuan] BETWEEN 1 AND 7");
                    table.CheckConstraint("CK_HO_SO_SoPhutBuoi", "[SoPhutBuoi] > 0");
                    table.ForeignKey(
                        name: "FK_HO_SO_TAP_LUYEN_HOI_VIEN_MaHoiVien",
                        column: x => x.MaHoiVien,
                        principalTable: "HOI_VIEN",
                        principalColumn: "MaHoiVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KE_HOACH_TAP_LUYEN",
                columns: table => new
                {
                    MaKeHoach = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHoiVien = table.Column<int>(type: "int", nullable: false),
                    TenKeHoach = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KE_HOACH_TAP_LUYEN", x => x.MaKeHoach);
                    table.CheckConstraint("CK_KE_HOACH_ThoiHan", "[NgayKetThuc] >= [NgayBatDau]");
                    table.ForeignKey(
                        name: "FK_KE_HOACH_TAP_LUYEN_HOI_VIEN_MaHoiVien",
                        column: x => x.MaHoiVien,
                        principalTable: "HOI_VIEN",
                        principalColumn: "MaHoiVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DANG_KY_GOI_TAP",
                columns: table => new
                {
                    MaDangKy = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHoiVien = table.Column<int>(type: "int", nullable: false),
                    MaGoiTap = table.Column<int>(type: "int", nullable: false),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DANG_KY_GOI_TAP", x => x.MaDangKy);
                    table.CheckConstraint("CK_DANG_KY_ThoiHan", "[NgayKetThuc] > [NgayBatDau]");
                    table.CheckConstraint("CK_DANG_KY_TongTien", "[TongTien] >= 0");
                    table.ForeignKey(
                        name: "FK_DANG_KY_GOI_TAP_GOI_TAP_MaGoiTap",
                        column: x => x.MaGoiTap,
                        principalTable: "GOI_TAP",
                        principalColumn: "MaGoiTap",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DANG_KY_GOI_TAP_HOI_VIEN_MaHoiVien",
                        column: x => x.MaHoiVien,
                        principalTable: "HOI_VIEN",
                        principalColumn: "MaHoiVien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DANG_KY_GOI_TAP_NHAN_VIEN_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NHAN_VIEN",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LICH_SU_BAO_TRI",
                columns: table => new
                {
                    MaBaoTri = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaThietBi = table.Column<int>(type: "int", nullable: false),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    NgayBaoTri = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ChiPhi = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LICH_SU_BAO_TRI", x => x.MaBaoTri);
                    table.CheckConstraint("CK_BAO_TRI_ChiPhi", "[ChiPhi] >= 0");
                    table.ForeignKey(
                        name: "FK_LICH_SU_BAO_TRI_NHAN_VIEN_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NHAN_VIEN",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LICH_SU_BAO_TRI_THIET_BI_MaThietBi",
                        column: x => x.MaThietBi,
                        principalTable: "THIET_BI",
                        principalColumn: "MaThietBi",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PHIEU_NHAP_XUAT_KHO",
                columns: table => new
                {
                    MaPhieu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    LoaiPhieu = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NgayLap = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NgayXacNhan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaNguoiXacNhan = table.Column<int>(type: "int", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PHIEU_NHAP_XUAT_KHO", x => x.MaPhieu);
                    table.ForeignKey(
                        name: "FK_PHIEU_NHAP_XUAT_KHO_NHAN_VIEN_MaNguoiXacNhan",
                        column: x => x.MaNguoiXacNhan,
                        principalTable: "NHAN_VIEN",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PHIEU_NHAP_XUAT_KHO_NHAN_VIEN_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NHAN_VIEN",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BUOI_TAP",
                columns: table => new
                {
                    MaBuoiTap = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaKeHoach = table.Column<int>(type: "int", nullable: false),
                    NgayTap = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThoiLuongPhut = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BUOI_TAP", x => x.MaBuoiTap);
                    table.CheckConstraint("CK_BUOI_TAP_ThoiLuong", "[ThoiLuongPhut] >= 0");
                    table.ForeignKey(
                        name: "FK_BUOI_TAP_KE_HOACH_TAP_LUYEN_MaKeHoach",
                        column: x => x.MaKeHoach,
                        principalTable: "KE_HOACH_TAP_LUYEN",
                        principalColumn: "MaKeHoach",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CHI_TIET_KE_HOACH",
                columns: table => new
                {
                    MaChiTietKeHoach = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaKeHoach = table.Column<int>(type: "int", nullable: false),
                    MaBaiTap = table.Column<int>(type: "int", nullable: false),
                    ThuTrongTuan = table.Column<int>(type: "int", nullable: false),
                    ThuTu = table.Column<int>(type: "int", nullable: false),
                    SoHiep = table.Column<int>(type: "int", nullable: false),
                    SoLan = table.Column<int>(type: "int", nullable: false),
                    NghiGiay = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHI_TIET_KE_HOACH", x => x.MaChiTietKeHoach);
                    table.CheckConstraint("CK_CHI_TIET_KE_HOACH_SoHiep", "[SoHiep] > 0");
                    table.CheckConstraint("CK_CHI_TIET_KE_HOACH_SoLan", "[SoLan] > 0");
                    table.CheckConstraint("CK_CHI_TIET_KE_HOACH_Thu", "[ThuTrongTuan] BETWEEN 1 AND 7");
                    table.ForeignKey(
                        name: "FK_CHI_TIET_KE_HOACH_BAI_TAP_MaBaiTap",
                        column: x => x.MaBaiTap,
                        principalTable: "BAI_TAP",
                        principalColumn: "MaBaiTap",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHI_TIET_KE_HOACH_KE_HOACH_TAP_LUYEN_MaKeHoach",
                        column: x => x.MaKeHoach,
                        principalTable: "KE_HOACH_TAP_LUYEN",
                        principalColumn: "MaKeHoach",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LICH_SU_CHECKIN",
                columns: table => new
                {
                    MaCheckIn = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDangKy = table.Column<int>(type: "int", nullable: false),
                    ThoiGianVao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PhuongThuc = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LICH_SU_CHECKIN", x => x.MaCheckIn);
                    table.ForeignKey(
                        name: "FK_LICH_SU_CHECKIN_DANG_KY_GOI_TAP_MaDangKy",
                        column: x => x.MaDangKy,
                        principalTable: "DANG_KY_GOI_TAP",
                        principalColumn: "MaDangKy",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CHI_TIET_PHIEU_KHO",
                columns: table => new
                {
                    MaChiTiet = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPhieu = table.Column<int>(type: "int", nullable: false),
                    MaHangHoa = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHI_TIET_PHIEU_KHO", x => x.MaChiTiet);
                    table.CheckConstraint("CK_CHI_TIET_PHIEU_DonGia", "[DonGia] >= 0");
                    table.CheckConstraint("CK_CHI_TIET_PHIEU_SoLuong", "[SoLuong] > 0");
                    table.ForeignKey(
                        name: "FK_CHI_TIET_PHIEU_KHO_HANG_HOA_MaHangHoa",
                        column: x => x.MaHangHoa,
                        principalTable: "HANG_HOA",
                        principalColumn: "MaHangHoa",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHI_TIET_PHIEU_KHO_PHIEU_NHAP_XUAT_KHO_MaPhieu",
                        column: x => x.MaPhieu,
                        principalTable: "PHIEU_NHAP_XUAT_KHO",
                        principalColumn: "MaPhieu",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "THANH_TOAN",
                columns: table => new
                {
                    MaThanhToan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDangKy = table.Column<int>(type: "int", nullable: true),
                    MaPhieuKho = table.Column<int>(type: "int", nullable: true),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgayThanhToan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PhuongThuc = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MaYeuCau = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_THANH_TOAN", x => x.MaThanhToan);
                    table.CheckConstraint("CK_THANH_TOAN_Nguon", "([MaDangKy] IS NOT NULL AND [MaPhieuKho] IS NULL) OR ([MaDangKy] IS NULL AND [MaPhieuKho] IS NOT NULL)");
                    table.CheckConstraint("CK_THANH_TOAN_SoTien", "[SoTien] >= 0");
                    table.ForeignKey(
                        name: "FK_THANH_TOAN_DANG_KY_GOI_TAP_MaDangKy",
                        column: x => x.MaDangKy,
                        principalTable: "DANG_KY_GOI_TAP",
                        principalColumn: "MaDangKy",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_THANH_TOAN_NHAN_VIEN_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NHAN_VIEN",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_THANH_TOAN_PHIEU_NHAP_XUAT_KHO_MaPhieuKho",
                        column: x => x.MaPhieuKho,
                        principalTable: "PHIEU_NHAP_XUAT_KHO",
                        principalColumn: "MaPhieu",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KET_QUA_PHAN_TICH_AI",
                columns: table => new
                {
                    MaPhanTich = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaBuoiTap = table.Column<int>(type: "int", nullable: false),
                    MaBaiTap = table.Column<int>(type: "int", nullable: false),
                    SoLanDemDuoc = table.Column<int>(type: "int", nullable: false),
                    NhanXet = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DoTinCay = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    NgayPhanTich = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PhienBanMoHinh = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KET_QUA_PHAN_TICH_AI", x => x.MaPhanTich);
                    table.CheckConstraint("CK_AI_DoTinCay", "[DoTinCay] BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_AI_SoLan", "[SoLanDemDuoc] >= 0");
                    table.ForeignKey(
                        name: "FK_KET_QUA_PHAN_TICH_AI_BAI_TAP_MaBaiTap",
                        column: x => x.MaBaiTap,
                        principalTable: "BAI_TAP",
                        principalColumn: "MaBaiTap",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KET_QUA_PHAN_TICH_AI_BUOI_TAP_MaBuoiTap",
                        column: x => x.MaBuoiTap,
                        principalTable: "BUOI_TAP",
                        principalColumn: "MaBuoiTap",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BUOI_TAP_MaKeHoach",
                table: "BUOI_TAP",
                column: "MaKeHoach");

            migrationBuilder.CreateIndex(
                name: "IX_CHI_TIET_KE_HOACH_MaBaiTap",
                table: "CHI_TIET_KE_HOACH",
                column: "MaBaiTap");

            migrationBuilder.CreateIndex(
                name: "IX_CHI_TIET_KE_HOACH_MaKeHoach",
                table: "CHI_TIET_KE_HOACH",
                column: "MaKeHoach");

            migrationBuilder.CreateIndex(
                name: "IX_CHI_TIET_PHIEU_KHO_MaHangHoa",
                table: "CHI_TIET_PHIEU_KHO",
                column: "MaHangHoa");

            migrationBuilder.CreateIndex(
                name: "IX_CHI_TIET_PHIEU_KHO_MaPhieu",
                table: "CHI_TIET_PHIEU_KHO",
                column: "MaPhieu");

            migrationBuilder.CreateIndex(
                name: "IX_DANG_KY_GOI_TAP_MaGoiTap",
                table: "DANG_KY_GOI_TAP",
                column: "MaGoiTap");

            migrationBuilder.CreateIndex(
                name: "IX_DANG_KY_GOI_TAP_MaHoiVien",
                table: "DANG_KY_GOI_TAP",
                column: "MaHoiVien");

            migrationBuilder.CreateIndex(
                name: "IX_DANG_KY_GOI_TAP_MaNhanVien",
                table: "DANG_KY_GOI_TAP",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_DONG_Y_SINH_TRAC_HOC_MaHoiVien",
                table: "DONG_Y_SINH_TRAC_HOC",
                column: "MaHoiVien",
                unique: true,
                filter: "[NgayRutLai] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DU_LIEU_KHUON_MAT_MaHoiVien",
                table: "DU_LIEU_KHUON_MAT",
                column: "MaHoiVien",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HO_SO_TAP_LUYEN_MaHoiVien",
                table: "HO_SO_TAP_LUYEN",
                column: "MaHoiVien",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HOI_VIEN_MaTaiKhoan",
                table: "HOI_VIEN",
                column: "MaTaiKhoan",
                unique: true,
                filter: "[MaTaiKhoan] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KE_HOACH_TAP_LUYEN_MaHoiVien",
                table: "KE_HOACH_TAP_LUYEN",
                column: "MaHoiVien");

            migrationBuilder.CreateIndex(
                name: "IX_KET_QUA_PHAN_TICH_AI_MaBaiTap",
                table: "KET_QUA_PHAN_TICH_AI",
                column: "MaBaiTap");

            migrationBuilder.CreateIndex(
                name: "IX_KET_QUA_PHAN_TICH_AI_MaBuoiTap",
                table: "KET_QUA_PHAN_TICH_AI",
                column: "MaBuoiTap");

            migrationBuilder.CreateIndex(
                name: "IX_LICH_SU_BAO_TRI_MaNhanVien",
                table: "LICH_SU_BAO_TRI",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_LICH_SU_BAO_TRI_MaThietBi",
                table: "LICH_SU_BAO_TRI",
                column: "MaThietBi");

            migrationBuilder.CreateIndex(
                name: "IX_LICH_SU_CHECKIN_MaDangKy",
                table: "LICH_SU_CHECKIN",
                column: "MaDangKy");

            migrationBuilder.CreateIndex(
                name: "IX_NHAN_VIEN_MaTaiKhoan",
                table: "NHAN_VIEN",
                column: "MaTaiKhoan",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PHIEU_NHAP_XUAT_KHO_MaNguoiXacNhan",
                table: "PHIEU_NHAP_XUAT_KHO",
                column: "MaNguoiXacNhan");

            migrationBuilder.CreateIndex(
                name: "IX_PHIEU_NHAP_XUAT_KHO_MaNhanVien",
                table: "PHIEU_NHAP_XUAT_KHO",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_THANH_TOAN_MaDangKy",
                table: "THANH_TOAN",
                column: "MaDangKy");

            migrationBuilder.CreateIndex(
                name: "IX_THANH_TOAN_MaNhanVien",
                table: "THANH_TOAN",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_THANH_TOAN_MaPhieuKho",
                table: "THANH_TOAN",
                column: "MaPhieuKho");

            migrationBuilder.CreateIndex(
                name: "IX_THANH_TOAN_MaYeuCau",
                table: "THANH_TOAN",
                column: "MaYeuCau",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CHI_TIET_KE_HOACH");

            migrationBuilder.DropTable(
                name: "CHI_TIET_PHIEU_KHO");

            migrationBuilder.DropTable(
                name: "DONG_Y_SINH_TRAC_HOC");

            migrationBuilder.DropTable(
                name: "DU_LIEU_KHUON_MAT");

            migrationBuilder.DropTable(
                name: "HO_SO_TAP_LUYEN");

            migrationBuilder.DropTable(
                name: "KET_QUA_PHAN_TICH_AI");

            migrationBuilder.DropTable(
                name: "LICH_SU_BAO_TRI");

            migrationBuilder.DropTable(
                name: "LICH_SU_CHECKIN");

            migrationBuilder.DropTable(
                name: "THANH_TOAN");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "HANG_HOA");

            migrationBuilder.DropTable(
                name: "BAI_TAP");

            migrationBuilder.DropTable(
                name: "BUOI_TAP");

            migrationBuilder.DropTable(
                name: "THIET_BI");

            migrationBuilder.DropTable(
                name: "DANG_KY_GOI_TAP");

            migrationBuilder.DropTable(
                name: "PHIEU_NHAP_XUAT_KHO");

            migrationBuilder.DropTable(
                name: "KE_HOACH_TAP_LUYEN");

            migrationBuilder.DropTable(
                name: "GOI_TAP");

            migrationBuilder.DropTable(
                name: "NHAN_VIEN");

            migrationBuilder.DropTable(
                name: "HOI_VIEN");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
