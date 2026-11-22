namespace FitGym.Application.Models;

// The 19 domain entities below correspond to the non-Identity tables in ERD.mdj.
// TAI_KHOAN, VAI_TRO and TAIKHOAN_VAITRO are implemented by ASP.NET Core Identity.
public sealed class NhanVien
{
    public int MaNhanVien { get; set; }
    public int MaTaiKhoan { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string ChucVu { get; set; } = string.Empty;
    public DateTime NgayVaoLam { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class HoiVien
{
    public int MaHoiVien { get; set; }
    public int? MaTaiKhoan { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public DateTime? NgaySinh { get; set; }
    public string? GioiTinh { get; set; }
    public string? DiaChi { get; set; }
    public string SoDienThoai { get; set; } = string.Empty;
    public DateTime NgayThamGia { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class GoiTap
{
    public int MaGoiTap { get; set; }
    public string TenGoi { get; set; } = string.Empty;
    public int ThoiHanThang { get; set; }
    public decimal GiaTien { get; set; }
    public string? MoTa { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class DangKyGoiTap
{
    public int MaDangKy { get; set; }
    public int? MaDangKyGoc { get; set; }
    public int MaHoiVien { get; set; }
    public int MaGoiTap { get; set; }
    public int? MaNhanVien { get; set; }
    public DateTime NgayBatDau { get; set; }
    public DateTime NgayKetThuc { get; set; }
    public decimal TongTien { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class ThanhToan
{
    public int MaThanhToan { get; set; }
    public int? MaDangKy { get; set; }
    public int? MaPhieuKho { get; set; }
    public int? MaNhanVien { get; set; }
    public decimal SoTien { get; set; }
    public DateTime NgayThanhToan { get; set; }
    public string PhuongThuc { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public string MaYeuCau { get; set; } = string.Empty;
}

public sealed class LichSuCheckIn
{
    public int MaCheckIn { get; set; }
    public string? MaSuKien { get; set; }
    public int MaDangKy { get; set; }
    public DateTime ThoiGianVao { get; set; }
    public string PhuongThuc { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class DuLieuKhuonMat
{
    public int MaDuLieuMat { get; set; }
    public int MaHoiVien { get; set; }
    public byte[] VectorKhuonMat { get; set; } = [];
    public string PhienBanMoHinh { get; set; } = string.Empty;
    public DateTime NgayTao { get; set; }
    public bool DangHoatDong { get; set; }
}

public sealed class DongYSinhTracHoc
{
    public int MaDongY { get; set; }
    public int MaHoiVien { get; set; }
    public DateTime NgayDongY { get; set; }
    public DateTime? NgayRutLai { get; set; }
    public string PhienBanNoiDung { get; set; } = string.Empty;
}

public sealed class HangHoa
{
    public int MaHangHoa { get; set; }
    public string TenHang { get; set; } = string.Empty;
    public string DonViTinh { get; set; } = string.Empty;
    public decimal GiaBan { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class PhieuNhapXuatKho
{
    public int MaPhieu { get; set; }
    public int MaNhanVien { get; set; }
    public string LoaiPhieu { get; set; } = string.Empty;
    public DateTime NgayLap { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public DateTime? NgayXacNhan { get; set; }
    public int? MaNguoiXacNhan { get; set; }
    public string? GhiChu { get; set; }
}

public sealed class ChiTietPhieuKho
{
    public int MaChiTiet { get; set; }
    public int MaPhieu { get; set; }
    public int MaHangHoa { get; set; }
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
}

public sealed class ThietBi
{
    public int MaThietBi { get; set; }
    public string TenThietBi { get; set; } = string.Empty;
    public string PhanLoai { get; set; } = string.Empty;
    public DateTime NgayMua { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class LichSuBaoTri
{
    public int MaBaoTri { get; set; }
    public int MaThietBi { get; set; }
    public int MaNhanVien { get; set; }
    public DateTime NgayBaoTri { get; set; }
    public string MoTa { get; set; } = string.Empty;
    public decimal ChiPhi { get; set; }
}

public sealed class HoSoTapLuyen
{
    public int MaHoSo { get; set; }
    public int MaHoiVien { get; set; }
    public string MucTieu { get; set; } = string.Empty;
    public string TrinhDo { get; set; } = string.Empty;
    public int SoNgayTuan { get; set; }
    public int SoPhutBuoi { get; set; }
    public string? DieuKienTap { get; set; }
}

public sealed class BaiTap
{
    public int MaBaiTap { get; set; }
    public string TenBaiTap { get; set; } = string.Empty;
    public string NhomCoChinh { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public string? LinkVideo { get; set; }
    public bool HoTroAI { get; set; }
}

public sealed class KeHoachTapLuyen
{
    public int MaKeHoach { get; set; }
    public int MaHoiVien { get; set; }
    public string TenKeHoach { get; set; } = string.Empty;
    public DateTime NgayBatDau { get; set; }
    public DateTime NgayKetThuc { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class ChiTietKeHoach
{
    public int MaChiTietKeHoach { get; set; }
    public int MaKeHoach { get; set; }
    public int MaBaiTap { get; set; }
    public int ThuTrongTuan { get; set; }
    public int ThuTu { get; set; }
    public int SoHiep { get; set; }
    public int SoLan { get; set; }
    public int NghiGiay { get; set; }
}

public sealed class BuoiTap
{
    public int MaBuoiTap { get; set; }
    public int MaKeHoach { get; set; }
    public DateTime NgayTap { get; set; }
    public int ThoiLuongPhut { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
}

public sealed class KetQuaPhanTichAI
{
    public int MaPhanTich { get; set; }
    public int MaBuoiTap { get; set; }
    public int MaBaiTap { get; set; }
    public int SoLanDemDuoc { get; set; }
    public string? NhanXet { get; set; }
    public decimal DoTinCay { get; set; }
    public DateTime NgayPhanTich { get; set; }
    public string PhienBanMoHinh { get; set; } = string.Empty;
}
