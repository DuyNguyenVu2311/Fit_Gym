using System.Text.Json;
using FitGym.Application.Features.CheckIns;
using FitGym.Application.Features.Members;
using FitGym.Application.Features.Registrations;
using FitGym.Application.Features.Warehouse;
using FitGym.Application.Models;
using FitGym.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var connectionString = "Server=.\\SQLEXPRESS;Database=FitGymDev;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
var options = new DbContextOptionsBuilder<FitGymDbContext>()
    .UseSqlServer(connectionString)
    .Options;

await using var db = new FitGymDbContext(options);

var jsonPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "sample_data.json");
if (!File.Exists(jsonPath))
{
    jsonPath = Path.GetFullPath("sample_data.json");
}
Console.WriteLine($"Reading sample data from: {jsonPath}");

var jsonString = await File.ReadAllTextAsync(jsonPath);
using var doc = JsonDocument.Parse(jsonString);
var root = doc.RootElement;

await using var transaction = await db.Database.BeginTransactionAsync();

try
{
    // 1. Goods
    Console.WriteLine("\n--- XỬ LÝ HÀNG HÓA ---");
    var goodsElement = root.GetProperty("goods");
    var goodsMap = new Dictionary<string, int>();

    foreach (var g in goodsElement.EnumerateArray())
    {
        var code = g.GetProperty("code").GetString()!;
        var name = g.GetProperty("name").GetString()!;
        var unit = g.GetProperty("unit").GetString()!;
        var price = g.GetProperty("price").GetDecimal();

        var existing = await db.HangHoa.FirstOrDefaultAsync(x => x.TenHang == name);
        if (existing != null)
        {
            Console.WriteLine($"[ĐÃ CÓ] {code}: {name} (ID: {existing.MaHangHoa})");
            goodsMap[code] = existing.MaHangHoa;
        }
        else
        {
            var item = new HangHoa
            {
                TenHang = name,
                DonViTinh = unit,
                GiaBan = price,
                TrangThai = GoodsStatuses.Active
            };
            db.HangHoa.Add(item);
            await db.SaveChangesAsync();
            goodsMap[code] = item.MaHangHoa;
            Console.WriteLine($"[THÊM MỚI] {code}: {name} -> ID {item.MaHangHoa} ({price:N0} VND)");
        }
    }

    // 2. Members
    Console.WriteLine("\n--- XỬ LÝ HỘI VIÊN VÀ GÓI ĐĂNG KÝ ---");
    var membersElement = root.GetProperty("members");
    var staff = await db.NhanVien.FirstOrDefaultAsync();
    var staffId = staff?.MaNhanVien ?? 80;

    int newMembersCount = 0;

    foreach (var m in membersElement.EnumerateArray())
    {
        var code = m.GetProperty("code").GetString()!;
        var name = m.GetProperty("name").GetString()!;
        var dob = DateTime.Parse(m.GetProperty("dob").GetString()!);
        var gender = m.GetProperty("gender").GetString()!;
        var phone = m.GetProperty("phone").GetString()!;
        var joinDate = DateTime.Parse(m.GetProperty("join_date").GetString()!);
        var statusStr = m.GetProperty("status").GetString()!;
        var packageId = m.GetProperty("package_id").GetInt32();
        var startDate = DateTime.Parse(m.GetProperty("start_date").GetString()!);
        var endDate = DateTime.Parse(m.GetProperty("end_date").GetString()!);
        var paymentStatusStr = m.GetProperty("payment_status").GetString()!;
        var faceDataStr = m.GetProperty("face_data").GetString()!;
        var scenario = m.GetProperty("scenario").GetString()!;

        var existing = await db.HoiVien.FirstOrDefaultAsync(x => x.SoDienThoai == phone);
        if (existing != null)
        {
            Console.WriteLine($"[BỎ QUA] Hội viên {code} - {name} ({phone}) đã tồn tại (ID: {existing.MaHoiVien}).");
            continue;
        }

        var memberStatus = statusStr == "Ngừng hoạt động" ? MemberStatuses.Inactive : MemberStatuses.Active;

        // A. Thêm HOI_VIEN
        var member = new HoiVien
        {
            HoTen = name,
            NgaySinh = dob,
            GioiTinh = gender,
            DiaChi = "Cơ sở 1 Fit_Gym",
            SoDienThoai = phone,
            NgayThamGia = joinDate,
            TrangThai = memberStatus
        };
        db.HoiVien.Add(member);
        await db.SaveChangesAsync();

        // B. Sinh trắc học & Khuôn mặt
        if (faceDataStr == "Đã có")
        {
            var consent = new DongYSinhTracHoc
            {
                MaHoiVien = member.MaHoiVien,
                NgayDongY = joinDate,
                NgayRutLai = null,
                PhienBanNoiDung = "v1.0"
            };
            db.DongYSinhTracHoc.Add(consent);

            var face = new DuLieuKhuonMat
            {
                MaHoiVien = member.MaHoiVien,
                VectorKhuonMat = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08],
                PhienBanMoHinh = "sample-data-v1",
                NgayTao = joinDate,
                DangHoatDong = true
            };
            db.DuLieuKhuonMat.Add(face);
            await db.SaveChangesAsync();
        }

        // C. Đăng ký gói tập
        var isPaid = paymentStatusStr == "Đã thanh toán";
        var regStatus = isPaid ? RegistrationStatuses.Active : RegistrationStatuses.PendingPayment;
        var totalAmount = packageId == 98 ? 300000m : 850000m;

        var reg = new DangKyGoiTap
        {
            MaDangKyGoc = null,
            MaHoiVien = member.MaHoiVien,
            MaGoiTap = packageId,
            MaNhanVien = staffId,
            NgayBatDau = startDate,
            NgayKetThuc = endDate,
            TongTien = totalAmount,
            TrangThai = regStatus
        };
        db.DangKyGoiTap.Add(reg);
        await db.SaveChangesAsync();

        // D. Thanh toán
        var payStatus = isPaid ? PaymentStatuses.Succeeded : PaymentStatuses.Pending;
        var payMethod = isPaid ? PaymentMethods.Cash : PaymentMethods.Qr;
        var payStaff = isPaid ? (int?)staffId : null;
        var reqCode = $"REQ-SMP-{code}-{Guid.NewGuid():N}"[..32];

        var payment = new ThanhToan
        {
            MaDangKy = reg.MaDangKy,
            MaPhieuKho = null,
            MaNhanVien = payStaff,
            SoTien = totalAmount,
            NgayThanhToan = startDate,
            PhuongThuc = payMethod,
            TrangThai = payStatus,
            MaYeuCau = reqCode
        };
        db.ThanhToan.Add(payment);
        await db.SaveChangesAsync();

        newMembersCount++;
        Console.WriteLine($"[THÀNH CÔNG] {code}: {name} -> Mã HV: {member.MaHoiVien}, Đăng ký: {reg.MaDangKy}, Thanh toán: {payment.MaThanhToan} ({payStatus}) | Kịch bản: {scenario}");
    }

    await transaction.CommitAsync();
    Console.WriteLine($"\n===> HOÀN TẤT GHI VÀO DATABASE! Đã thêm mới: {newMembersCount} hội viên, hàng hóa: {goodsMap.Count}.");
}
catch (Exception ex)
{
    await transaction.RollbackAsync();
    Console.WriteLine($"LỖI: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner: {ex.InnerException.Message}");
    }
}
