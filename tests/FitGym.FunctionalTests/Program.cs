using FitGym.Application.Features.CheckIns;
using FitGym.Application.Features.Registrations;
using FitGym.Application.Models;
using FitGym.Infrastructure.Features.CheckIns;
using FitGym.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var connectionString = Environment.GetEnvironmentVariable("FITGYM_TEST_CONNECTION")
    ?? throw new InvalidOperationException("FITGYM_TEST_CONNECTION is required.");

var options = new DbContextOptionsBuilder<FitGymDbContext>()
    .UseSqlServer(connectionString)
    .Options;
await using var db = new FitGymDbContext(options);

var member = await db.HoiVien.SingleAsync(x => x.HoTen == "__FITGYM_REGISTRATION_MEMBER__");
var registration = await db.DangKyGoiTap
    .Where(x => x.MaHoiVien == member.MaHoiVien && x.TrangThai == RegistrationStatuses.Active)
    .OrderBy(x => x.MaDangKy)
    .FirstAsync();
var service = new CheckInService(db);

var manual = await service.RecordManualAsync(registration.MaDangKy, "FunctionalTest");
Ensure(manual.Succeeded, "Valid paid membership must allow manual check-in.");

var originalStart = registration.NgayBatDau;
var originalEnd = registration.NgayKetThuc;
registration.NgayBatDau = DateTime.Today.AddMonths(-2);
registration.NgayKetThuc = DateTime.Today.AddDays(-1);
await db.SaveChangesAsync();
var expired = await service.RecordManualAsync(registration.MaDangKy, "FunctionalTest");
Ensure(!expired.Succeeded && expired.Error?.Contains("hết hạn", StringComparison.OrdinalIgnoreCase) == true,
    "Expired membership must be blocked.");
registration.NgayBatDau = originalStart;
registration.NgayKetThuc = originalEnd;
await db.SaveChangesAsync();

if (!await db.DuLieuKhuonMat.AnyAsync(x => x.MaHoiVien == member.MaHoiVien))
{
    db.DuLieuKhuonMat.Add(new DuLieuKhuonMat
    {
        MaHoiVien = member.MaHoiVien,
        VectorKhuonMat = [1, 2, 3],
        PhienBanMoHinh = "functional-test-fixture",
        NgayTao = DateTime.UtcNow,
        DangHoatDong = true
    });
    await db.SaveChangesAsync();
}

var first = await service.RecordFaceAttemptAsync(registration.MaDangKy, false);
var second = await service.RecordFaceAttemptAsync(registration.MaDangKy, false);
var third = await service.RecordFaceAttemptAsync(registration.MaDangKy, false);
Ensure(!first.RequiresStaffAssistance && first.FailedAttempts == 1, "First failed face attempt must allow retry.");
Ensure(!second.RequiresStaffAssistance && second.FailedAttempts == 2, "Second failed face attempt must allow retry.");
Ensure(third.RequiresStaffAssistance && third.FailedAttempts == 3, "Third failed face attempt must escalate to staff.");

var success = await service.RecordFaceAttemptAsync(registration.MaDangKy, true);
Ensure(!success.Succeeded && success.RequiresStaffAssistance, "A fourth face attempt in the same window must stop for staff assistance.");
await db.LichSuCheckIn.Where(x => x.MaDangKy == registration.MaDangKy && x.PhuongThuc == CheckInMethods.Face && x.TrangThai == CheckInStatuses.FaceFailed)
    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ThoiGianVao, DateTime.Now.AddMinutes(-6)));
var nextSession = await service.RecordFaceAttemptAsync(registration.MaDangKy, true);
Ensure(nextSession.Succeeded && !nextSession.RequiresStaffAssistance, "A new attempt window can accept a valid face match.");

Console.WriteLine("check-in functional scenarios: PASS");

static void Ensure(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
