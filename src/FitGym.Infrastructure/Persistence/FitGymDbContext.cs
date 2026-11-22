using FitGym.Application.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitGym.Infrastructure.Persistence;

public sealed class AppUser : IdentityUser<int>
{
    public string TrangThai { get; set; } = "HoatDong";
}

public sealed class FitGymDbContext(DbContextOptions<FitGymDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<int>, int>(options)
{
    public DbSet<DonViSuDung> DonViSuDung => Set<DonViSuDung>();
    public DbSet<CoSo> CoSo => Set<CoSo>();
    public DbSet<NhanVien> NhanVien => Set<NhanVien>();
    public DbSet<HoiVien> HoiVien => Set<HoiVien>();
    public DbSet<GoiTap> GoiTap => Set<GoiTap>();
    public DbSet<DangKyGoiTap> DangKyGoiTap => Set<DangKyGoiTap>();
    public DbSet<ThanhToan> ThanhToan => Set<ThanhToan>();
    public DbSet<LichSuCheckIn> LichSuCheckIn => Set<LichSuCheckIn>();
    public DbSet<DuLieuKhuonMat> DuLieuKhuonMat => Set<DuLieuKhuonMat>();
    public DbSet<DongYSinhTracHoc> DongYSinhTracHoc => Set<DongYSinhTracHoc>();
    public DbSet<HangHoa> HangHoa => Set<HangHoa>();
    public DbSet<PhieuNhapXuatKho> PhieuNhapXuatKho => Set<PhieuNhapXuatKho>();
    public DbSet<ChiTietPhieuKho> ChiTietPhieuKho => Set<ChiTietPhieuKho>();
    public DbSet<ThietBi> ThietBi => Set<ThietBi>();
    public DbSet<LichSuBaoTri> LichSuBaoTri => Set<LichSuBaoTri>();
    public DbSet<HoSoTapLuyen> HoSoTapLuyen => Set<HoSoTapLuyen>();
    public DbSet<BaiTap> BaiTap => Set<BaiTap>();
    public DbSet<KeHoachTapLuyen> KeHoachTapLuyen => Set<KeHoachTapLuyen>();
    public DbSet<ChiTietKeHoach> ChiTietKeHoach => Set<ChiTietKeHoach>();
    public DbSet<BuoiTap> BuoiTap => Set<BuoiTap>();
    public DbSet<KetQuaPhanTichAI> KetQuaPhanTichAI => Set<KetQuaPhanTichAI>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DonViSuDung>(e =>
        {
            e.ToTable("DON_VI_SU_DUNG", table => table.HasCheckConstraint("CK_DON_VI_SINGLE_INSTALL", "[MaDonVi] = 1"));
            e.HasKey(x => x.MaDonVi);
            e.Property(x => x.MaDonVi).ValueGeneratedNever();
            e.Property(x => x.TenDonVi).HasMaxLength(200);
            e.Property(x => x.SoDienThoai).HasMaxLength(30);
            e.Property(x => x.LogoContentType).HasMaxLength(32);
        });
        modelBuilder.Entity<CoSo>(e =>
        {
            e.ToTable("CO_SO");
            e.HasKey(x => x.MaCoSo);
            e.Property(x => x.TenCoSo).HasMaxLength(200);
            e.Property(x => x.DiaChi).HasMaxLength(500);
            e.HasIndex(x => new { x.MaDonVi, x.TenCoSo }).IsUnique();
            e.HasOne<DonViSuDung>().WithMany().HasForeignKey(x => x.MaDonVi).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NhanVien>(e =>
        {
            e.ToTable("NHAN_VIEN");
            e.HasKey(x => x.MaNhanVien);
            e.HasOne<AppUser>().WithOne().HasForeignKey<NhanVien>(x => x.MaTaiKhoan).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<HoiVien>(e =>
        {
            e.ToTable("HOI_VIEN");
            e.HasKey(x => x.MaHoiVien);
            e.HasOne<AppUser>().WithOne().HasForeignKey<HoiVien>(x => x.MaTaiKhoan).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<GoiTap>(e =>
        {
            e.ToTable("GOI_TAP", t =>
            {
                t.HasCheckConstraint("CK_GOI_TAP_ThoiHan", "[ThoiHanThang] > 0");
                t.HasCheckConstraint("CK_GOI_TAP_Gia", "[GiaTien] >= 0");
            });
            e.HasKey(x => x.MaGoiTap);
        });
        modelBuilder.Entity<DangKyGoiTap>(e =>
        {
            e.ToTable("DANG_KY_GOI_TAP", t =>
            {
                t.HasCheckConstraint("CK_DANG_KY_ThoiHan", "[NgayKetThuc] > [NgayBatDau]");
                t.HasCheckConstraint("CK_DANG_KY_TongTien", "[TongTien] >= 0");
            });
            e.HasKey(x => x.MaDangKy);
            e.HasOne<DangKyGoiTap>().WithMany().HasForeignKey(x => x.MaDangKyGoc).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<HoiVien>().WithMany().HasForeignKey(x => x.MaHoiVien).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<GoiTap>().WithMany().HasForeignKey(x => x.MaGoiTap).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<NhanVien>().WithMany().HasForeignKey(x => x.MaNhanVien).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ThanhToan>(e =>
        {
            e.ToTable("THANH_TOAN", t =>
            {
                t.HasCheckConstraint("CK_THANH_TOAN_Nguon", "([MaDangKy] IS NOT NULL AND [MaPhieuKho] IS NULL) OR ([MaDangKy] IS NULL AND [MaPhieuKho] IS NOT NULL)");
                t.HasCheckConstraint("CK_THANH_TOAN_SoTien", "[SoTien] >= 0");
            });
            e.HasKey(x => x.MaThanhToan);
            e.HasIndex(x => x.MaYeuCau).IsUnique();
            e.HasOne<DangKyGoiTap>().WithMany().HasForeignKey(x => x.MaDangKy).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<PhieuNhapXuatKho>().WithMany().HasForeignKey(x => x.MaPhieuKho).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<NhanVien>().WithMany().HasForeignKey(x => x.MaNhanVien).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<LichSuCheckIn>(e =>
        {
            e.ToTable("LICH_SU_CHECKIN");
            e.HasKey(x => x.MaCheckIn);
            e.Property(x => x.MaSuKien).HasMaxLength(64);
            e.HasIndex(x => x.MaSuKien).IsUnique().HasFilter("[MaSuKien] IS NOT NULL");
            e.HasOne<DangKyGoiTap>().WithMany().HasForeignKey(x => x.MaDangKy).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<DuLieuKhuonMat>(e =>
        {
            e.ToTable("DU_LIEU_KHUON_MAT");
            e.HasKey(x => x.MaDuLieuMat);
            e.HasOne<HoiVien>().WithOne().HasForeignKey<DuLieuKhuonMat>(x => x.MaHoiVien).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<DongYSinhTracHoc>(e =>
        {
            e.ToTable("DONG_Y_SINH_TRAC_HOC", t =>
                t.HasCheckConstraint("CK_DONG_Y_NgayRutLai", "[NgayRutLai] IS NULL OR [NgayRutLai] >= [NgayDongY]"));
            e.HasKey(x => x.MaDongY);
            e.HasIndex(x => x.MaHoiVien).IsUnique().HasFilter("[NgayRutLai] IS NULL");
            e.HasOne<HoiVien>().WithMany().HasForeignKey(x => x.MaHoiVien).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<HangHoa>(e =>
        {
            e.ToTable("HANG_HOA", t => t.HasCheckConstraint("CK_HANG_HOA_GiaBan", "[GiaBan] >= 0"));
            e.HasKey(x => x.MaHangHoa);
        });
        modelBuilder.Entity<PhieuNhapXuatKho>(e =>
        {
            e.ToTable("PHIEU_NHAP_XUAT_KHO");
            e.HasKey(x => x.MaPhieu);
            e.HasOne<NhanVien>().WithMany().HasForeignKey(x => x.MaNhanVien).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<NhanVien>().WithMany().HasForeignKey(x => x.MaNguoiXacNhan).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ChiTietPhieuKho>(e =>
        {
            e.ToTable("CHI_TIET_PHIEU_KHO", t =>
            {
                t.HasCheckConstraint("CK_CHI_TIET_PHIEU_SoLuong", "[SoLuong] > 0");
                t.HasCheckConstraint("CK_CHI_TIET_PHIEU_DonGia", "[DonGia] >= 0");
            });
            e.HasKey(x => x.MaChiTiet);
            e.HasOne<PhieuNhapXuatKho>().WithMany().HasForeignKey(x => x.MaPhieu).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<HangHoa>().WithMany().HasForeignKey(x => x.MaHangHoa).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ThietBi>(e =>
        {
            e.ToTable("THIET_BI");
            e.HasKey(x => x.MaThietBi);
        });
        modelBuilder.Entity<LichSuBaoTri>(e =>
        {
            e.ToTable("LICH_SU_BAO_TRI", t => t.HasCheckConstraint("CK_BAO_TRI_ChiPhi", "[ChiPhi] >= 0"));
            e.HasKey(x => x.MaBaoTri);
            e.HasOne<ThietBi>().WithMany().HasForeignKey(x => x.MaThietBi).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<NhanVien>().WithMany().HasForeignKey(x => x.MaNhanVien).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<HoSoTapLuyen>(e =>
        {
            e.ToTable("HO_SO_TAP_LUYEN", t =>
            {
                t.HasCheckConstraint("CK_HO_SO_SoNgayTuan", "[SoNgayTuan] BETWEEN 1 AND 7");
                t.HasCheckConstraint("CK_HO_SO_SoPhutBuoi", "[SoPhutBuoi] > 0");
            });
            e.HasKey(x => x.MaHoSo);
            e.HasOne<HoiVien>().WithOne().HasForeignKey<HoSoTapLuyen>(x => x.MaHoiVien).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<BaiTap>(e =>
        {
            e.ToTable("BAI_TAP");
            e.HasKey(x => x.MaBaiTap);
        });
        modelBuilder.Entity<KeHoachTapLuyen>(e =>
        {
            e.ToTable("KE_HOACH_TAP_LUYEN", t =>
                t.HasCheckConstraint("CK_KE_HOACH_ThoiHan", "[NgayKetThuc] >= [NgayBatDau]"));
            e.HasKey(x => x.MaKeHoach);
            e.HasOne<HoiVien>().WithMany().HasForeignKey(x => x.MaHoiVien).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ChiTietKeHoach>(e =>
        {
            e.ToTable("CHI_TIET_KE_HOACH", t =>
            {
                t.HasCheckConstraint("CK_CHI_TIET_KE_HOACH_Thu", "[ThuTrongTuan] BETWEEN 1 AND 7");
                t.HasCheckConstraint("CK_CHI_TIET_KE_HOACH_SoHiep", "[SoHiep] > 0");
                t.HasCheckConstraint("CK_CHI_TIET_KE_HOACH_SoLan", "[SoLan] > 0");
            });
            e.HasKey(x => x.MaChiTietKeHoach);
            e.HasOne<KeHoachTapLuyen>().WithMany().HasForeignKey(x => x.MaKeHoach).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<BaiTap>().WithMany().HasForeignKey(x => x.MaBaiTap).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<BuoiTap>(e =>
        {
            e.ToTable("BUOI_TAP", t => t.HasCheckConstraint("CK_BUOI_TAP_ThoiLuong", "[ThoiLuongPhut] >= 0"));
            e.HasKey(x => x.MaBuoiTap);
            e.HasOne<KeHoachTapLuyen>().WithMany().HasForeignKey(x => x.MaKeHoach).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<KetQuaPhanTichAI>(e =>
        {
            e.ToTable("KET_QUA_PHAN_TICH_AI", t =>
            {
                t.HasCheckConstraint("CK_AI_SoLan", "[SoLanDemDuoc] >= 0");
                t.HasCheckConstraint("CK_AI_DoTinCay", "[DoTinCay] BETWEEN 0 AND 1");
            });
            e.HasKey(x => x.MaPhanTich);
            e.HasOne<BuoiTap>().WithMany().HasForeignKey(x => x.MaBuoiTap).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<BaiTap>().WithMany().HasForeignKey(x => x.MaBaiTap).OnDelete(DeleteBehavior.Restrict);
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(string) && property.GetMaxLength() is null)
                    property.SetMaxLength(256);
                if (property.ClrType == typeof(decimal))
                    property.SetColumnType(property.Name == "DoTinCay" ? "decimal(5,4)" : "decimal(18,2)");
            }
        }

        modelBuilder.Entity<ThanhToan>().Property(x => x.MaYeuCau).HasMaxLength(64);
        modelBuilder.Entity<HangHoa>().Property(x => x.TenHang).HasMaxLength(200);
        modelBuilder.Entity<GoiTap>().Property(x => x.MoTa).HasMaxLength(2000);
        modelBuilder.Entity<BaiTap>().Property(x => x.MoTa).HasMaxLength(2000);
        modelBuilder.Entity<BaiTap>().Property(x => x.LinkVideo).HasMaxLength(1000);
        modelBuilder.Entity<KetQuaPhanTichAI>().Property(x => x.NhanXet).HasMaxLength(2000);
    }
}
