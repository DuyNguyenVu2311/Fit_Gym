using FitGym.Application.Features.Training;
using FitGym.Application.Models;
using FitGym.Infrastructure.Features.Training;
using FitGym.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

const string marker = "__FITGYM_TRAINING_TEST__";
var connectionString = Environment.GetEnvironmentVariable("FITGYM_TEST_CONNECTION")
    ?? "Server=.\\SQLEXPRESS;Database=FitGymDev;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
var options = new DbContextOptionsBuilder<FitGymDbContext>().UseSqlServer(connectionString).Options;
await using var db = new FitGymDbContext(options);
await CleanupAsync();

try
{
    var member = new HoiVien
    {
        HoTen = marker + " MEMBER",
        SoDienThoai = "0900000001",
        NgayThamGia = DateTime.Today,
        TrangThai = "HoatDong"
    };
    db.HoiVien.Add(member);
    await db.SaveChangesAsync();
    var service = new TrainingFoundationService(db);

    var invalidDays = await service.UpsertProfileAsync(new TrainingProfileRequest(
        member.MaHoiVien, "Tăng sức mạnh", "Mới bắt đầu", 8, 60, null));
    Ensure(!invalidDays.Succeeded, "More than seven training days must be rejected.");

    var profileResult = await service.UpsertProfileAsync(new TrainingProfileRequest(
        member.MaHoiVien, "Tăng sức mạnh", "Mới bắt đầu", 3, 60, "Đau gối do hội viên tự khai"));
    Ensure(profileResult.Succeeded, "A valid member training profile must be saved.");
    var profile = await service.GetProfileAsync(member.MaHoiVien);
    Ensure(profile is { SoNgayTuan: 3, SoPhutBuoi: 60 }, "Saved profile must be returned exactly.");

    var invalidVideo = await service.CreateExerciseAsync(new ExerciseRequest(
        0, marker + " SQUAT", "Chân", null, "not-a-url", false));
    Ensure(!invalidVideo.Succeeded, "Invalid exercise video URL must be rejected.");

    var exercise = await service.CreateExerciseAsync(new ExerciseRequest(
        0, marker + " SQUAT", "Chân", "Mô tả kiểm thử", "https://example.test/squat", false));
    Ensure(exercise.Succeeded, "A valid exercise must be created.");
    var duplicate = await service.CreateExerciseAsync(new ExerciseRequest(
        0, marker + " SQUAT", "Chân", null, null, false));
    Ensure(!duplicate.Succeeded, "Duplicate exercise names must be rejected.");

    Console.WriteLine("training foundation functional scenarios: PASS");
}
finally
{
    await CleanupAsync();
}

async Task CleanupAsync()
{
    var memberIds = await db.HoiVien.Where(x => x.HoTen.StartsWith(marker)).Select(x => x.MaHoiVien).ToListAsync();
    db.HoSoTapLuyen.RemoveRange(db.HoSoTapLuyen.Where(x => memberIds.Contains(x.MaHoiVien)));
    db.HoiVien.RemoveRange(db.HoiVien.Where(x => memberIds.Contains(x.MaHoiVien)));
    db.BaiTap.RemoveRange(db.BaiTap.Where(x => x.TenBaiTap.StartsWith(marker)));
    await db.SaveChangesAsync();
}

static void Ensure(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
