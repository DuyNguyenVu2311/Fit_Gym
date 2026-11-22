using FitGym.Application.Common;
using FitGym.Application.Features.CheckIns;
using FitGym.Application.Features.Members;
using FitGym.Application.Features.Registrations;
using FitGym.Application.Models;
using FitGym.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace FitGym.Infrastructure.Features.CheckIns;

public sealed class CheckInService(FitGymDbContext dbContext) : ICheckInService
{
    public async Task<IReadOnlyList<CheckInCandidate>> GetEligibleCandidatesAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var query =
            from registration in dbContext.DangKyGoiTap.AsNoTracking()
            join member in dbContext.HoiVien.AsNoTracking() on registration.MaHoiVien equals member.MaHoiVien
            join package in dbContext.GoiTap.AsNoTracking() on registration.MaGoiTap equals package.MaGoiTap
            where registration.TrangThai == RegistrationStatuses.Active
                  && registration.NgayBatDau <= today
                  && registration.NgayKetThuc >= today
                  && member.TrangThai == MemberStatuses.Active
                  && dbContext.ThanhToan.Any(payment =>
                      payment.MaDangKy == registration.MaDangKy &&
                      payment.TrangThai == PaymentStatuses.Succeeded)
            select new { Registration = registration, Member = member, Package = package };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Member.HoTen.Contains(term) || x.Member.SoDienThoai.Contains(term));
        }

        return await query
            .OrderBy(x => x.Member.HoTen)
            .Select(x => new CheckInCandidate(
                x.Registration.MaDangKy,
                x.Member.MaHoiVien,
                x.Member.HoTen,
                x.Member.SoDienThoai,
                x.Package.TenGoi,
                x.Registration.NgayKetThuc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CheckInHistoryItem>> GetHistoryAsync(
        DateTime date,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var startUtc = date.Date;
        var endUtc = startUtc.AddDays(1);
        var query =
            from checkIn in dbContext.LichSuCheckIn.AsNoTracking()
            join registration in dbContext.DangKyGoiTap.AsNoTracking() on checkIn.MaDangKy equals registration.MaDangKy
            join member in dbContext.HoiVien.AsNoTracking() on registration.MaHoiVien equals member.MaHoiVien
            join package in dbContext.GoiTap.AsNoTracking() on registration.MaGoiTap equals package.MaGoiTap
            where checkIn.ThoiGianVao >= startUtc && checkIn.ThoiGianVao < endUtc
            select new { CheckIn = checkIn, Member = member, Package = package };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Member.HoTen.Contains(term) || x.Member.SoDienThoai.Contains(term));
        }

        return await query
            .OrderByDescending(x => x.CheckIn.ThoiGianVao)
            .Select(x => new CheckInHistoryItem(
                x.CheckIn.MaCheckIn,
                x.CheckIn.MaDangKy,
                x.Member.HoTen,
                x.Package.TenGoi,
                x.CheckIn.ThoiGianVao,
                x.CheckIn.PhuongThuc,
                x.CheckIn.TrangThai))
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<int>> RecordManualAsync(
        int registrationId,
        string assistanceReason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assistanceReason))
            return OperationResult<int>.Failure("Vui lòng chọn lý do nhân viên hỗ trợ check-in.");
        var eligibility = await ValidateEligibilityAsync(registrationId, cancellationToken);
        if (!eligibility.Succeeded) return OperationResult<int>.Failure(eligibility.Error!);

        var checkIn = new LichSuCheckIn
        {
            MaDangKy = registrationId,
            ThoiGianVao = DateTime.Now,
            PhuongThuc = CheckInMethods.Manual,
            TrangThai = CheckInStatuses.Succeeded
        };
        dbContext.LichSuCheckIn.Add(checkIn);
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Success(checkIn.MaCheckIn);
    }

    public Task<CheckInResult> RecordFaceAttemptAsync(
        int registrationId,
        bool matched,
        CancellationToken cancellationToken = default) => HandleFaceAttemptAsync(registrationId, matched, true, null, cancellationToken);

    public Task<CheckInResult> EvaluateGateAccessAsync(
        int registrationId,
        bool matched,
        CancellationToken cancellationToken = default) => HandleFaceAttemptAsync(registrationId, matched, false, null, cancellationToken);

    public async Task<CheckInResult> RecordGateFailureAsync(
        int registrationId,
        string eventId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventId) || eventId.Length != 32 || !eventId.All(Uri.IsHexDigit))
            return new CheckInResult(false, false, 0, null, "Mã sự kiện nhận diện không hợp lệ.");
        var key = eventId.ToUpperInvariant();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var existing = await dbContext.LichSuCheckIn.FromSqlInterpolated($"SELECT * FROM LICH_SU_CHECKIN WITH (UPDLOCK, HOLDLOCK) WHERE MaSuKien = {key}")
            .SingleOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            if (existing.MaDangKy != registrationId || existing.TrangThai != CheckInStatuses.FaceFailed)
                return new CheckInResult(false, true, 0, null, "Mã sự kiện đã thuộc một lượt khác.");
            var windowStart = existing.ThoiGianVao.AddMinutes(-5);
            var lastSuccess = await dbContext.LichSuCheckIn.AsNoTracking()
                .Where(x => x.MaDangKy == registrationId && x.PhuongThuc == CheckInMethods.Face &&
                            x.TrangThai == CheckInStatuses.Succeeded && x.ThoiGianVao >= windowStart && x.ThoiGianVao <= existing.ThoiGianVao)
                .MaxAsync(x => (DateTime?)x.ThoiGianVao, cancellationToken);
            if (lastSuccess.HasValue) windowStart = lastSuccess.Value;
            var count = await dbContext.LichSuCheckIn.AsNoTracking().CountAsync(x =>
                x.MaDangKy == registrationId && x.PhuongThuc == CheckInMethods.Face &&
                x.TrangThai == CheckInStatuses.FaceFailed && x.ThoiGianVao >= windowStart &&
                x.ThoiGianVao <= existing.ThoiGianVao, cancellationToken);
            return new CheckInResult(false, count >= 3, count, existing.MaCheckIn,
                count >= 3 ? "Vui lòng liên hệ nhân viên để được hỗ trợ." : "Vui lòng thử lại.");
        }
        var result = await HandleFaceAttemptAsync(registrationId, false, false, key, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<CheckInResult> HandleFaceAttemptAsync(
        int registrationId,
        bool matched,
        bool recordOnMatch,
        string? eventId,
        CancellationToken cancellationToken)
    {
        var eligibility = await ValidateEligibilityAsync(registrationId, cancellationToken);
        if (!eligibility.Succeeded)
            return new CheckInResult(false, true, 0, null, eligibility.Error!);

        var memberId = eligibility.MemberId!.Value;
        var hasConsent = await dbContext.DongYSinhTracHoc.AsNoTracking()
            .AnyAsync(x => x.MaHoiVien == memberId && x.NgayRutLai == null, cancellationToken);
        var hasFaceData = await dbContext.DuLieuKhuonMat.AsNoTracking()
            .AnyAsync(x => x.MaHoiVien == memberId && x.DangHoatDong, cancellationToken);
        if (!hasConsent || !hasFaceData)
            return new CheckInResult(false, true, 0, null, "Chưa có đồng ý sinh trắc học hoặc dữ liệu khuôn mặt đang hoạt động. Vui lòng liên hệ nhân viên.");

        var now = DateTime.Now;
        var attemptWindowStart = now.AddMinutes(-5);
        var lastSuccess = await dbContext.LichSuCheckIn.AsNoTracking()
            .Where(x => x.MaDangKy == registrationId &&
                        x.PhuongThuc == CheckInMethods.Face &&
                        x.TrangThai == CheckInStatuses.Succeeded &&
                        x.ThoiGianVao >= attemptWindowStart)
            .MaxAsync(x => (DateTime?)x.ThoiGianVao, cancellationToken);
        if (lastSuccess.HasValue) attemptWindowStart = lastSuccess.Value;

        var previousFailures = await dbContext.LichSuCheckIn.AsNoTracking()
            .CountAsync(x => x.MaDangKy == registrationId &&
                             x.PhuongThuc == CheckInMethods.Face &&
                             x.TrangThai == CheckInStatuses.FaceFailed &&
                             x.ThoiGianVao >= attemptWindowStart,
                cancellationToken);

        if (previousFailures >= 3)
            return new CheckInResult(false, true, previousFailures, null,
                "Đã thử nhận diện 3 lần. Vui lòng liên hệ nhân viên để được hỗ trợ; người tiếp theo có thể sử dụng máy check-in.");

        if (!matched)
        {
            var failure = new LichSuCheckIn
            {
                MaSuKien = eventId,
                MaDangKy = registrationId,
                ThoiGianVao = now,
                PhuongThuc = CheckInMethods.Face,
                TrangThai = CheckInStatuses.FaceFailed
            };
            dbContext.LichSuCheckIn.Add(failure);
            await dbContext.SaveChangesAsync(cancellationToken);
            var failedAttempts = previousFailures + 1;
            if (failedAttempts >= 3)
                return new CheckInResult(false, true, failedAttempts, failure.MaCheckIn, "Nhận diện không thành công sau 3 lần. Vui lòng nhường lối và liên hệ nhân viên hỗ trợ.");
            return new CheckInResult(false, false, failedAttempts, failure.MaCheckIn, "Chưa nhận diện được. Vui lòng tháo kính, vén tóc và thử lại.");
        }

        if (!recordOnMatch)
            return new CheckInResult(true, false, 0, null, "Đủ điều kiện vào phòng.");

        var success = new LichSuCheckIn
        {
            MaDangKy = registrationId,
            ThoiGianVao = now,
            PhuongThuc = CheckInMethods.Face,
            TrangThai = CheckInStatuses.Succeeded
        };
        dbContext.LichSuCheckIn.Add(success);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CheckInResult(true, false, 0, success.MaCheckIn, "Check-in thành công.");
    }

    public async Task<OperationResult<int>> RecordGateEntryAsync(
        int registrationId,
        string eventId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventId) || eventId.Length != 32 || !eventId.All(Uri.IsHexDigit))
            return OperationResult<int>.Failure("Mã sự kiện mở cửa không hợp lệ.");
        var key = eventId.ToUpperInvariant();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var existing = await dbContext.LichSuCheckIn.FromSqlInterpolated($"SELECT * FROM LICH_SU_CHECKIN WITH (UPDLOCK, HOLDLOCK) WHERE MaSuKien = {key}")
            .SingleOrDefaultAsync(cancellationToken);
        if (existing is not null)
            return existing.MaDangKy == registrationId && existing.TrangThai == CheckInStatuses.Succeeded
                ? OperationResult<int>.Success(existing.MaCheckIn)
                : OperationResult<int>.Failure("Mã sự kiện đã thuộc một lượt khác.");

        var access = await EvaluateGateAccessAsync(registrationId, true, cancellationToken);
        if (!access.Succeeded) return OperationResult<int>.Failure(access.Message);
        var entry = new LichSuCheckIn
        {
            MaSuKien = key,
            MaDangKy = registrationId,
            ThoiGianVao = DateTime.Now,
            PhuongThuc = CheckInMethods.Face,
            TrangThai = CheckInStatuses.Succeeded
        };
        dbContext.LichSuCheckIn.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return OperationResult<int>.Success(entry.MaCheckIn);
    }

    private async Task<EligibilityResult> ValidateEligibilityAsync(
        int registrationId,
        CancellationToken cancellationToken)
    {
        var registration = await dbContext.DangKyGoiTap.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaDangKy == registrationId, cancellationToken);
        if (registration is null) return EligibilityResult.Fail("Không tìm thấy đăng ký gói tập.");
        var paid = await dbContext.ThanhToan.AsNoTracking().AnyAsync(
            x => x.MaDangKy == registrationId && x.TrangThai == PaymentStatuses.Succeeded,
            cancellationToken);
        var validity = RegistrationValidity.Evaluate(registration.TrangThai, registration.NgayBatDau, registration.NgayKetThuc, paid, DateTime.Today);
        if (validity == RegistrationValidity.Cancelled) return EligibilityResult.Fail("Đăng ký đã hủy.");
        if (validity == RegistrationValidity.PaymentUnconfirmed) return EligibilityResult.Fail("Đăng ký chưa được thanh toán thành công.");
        if (validity == RegistrationValidity.NotStarted) return EligibilityResult.Fail("Gói tập chưa đến ngày bắt đầu.");
        if (validity == RegistrationValidity.Expired) return EligibilityResult.Fail("Gói tập đã hết hạn; cần gia hạn trước khi check-in.");
        var memberActive = await dbContext.HoiVien.AsNoTracking().AnyAsync(
            x => x.MaHoiVien == registration.MaHoiVien && x.TrangThai == MemberStatuses.Active,
            cancellationToken);
        return memberActive
            ? EligibilityResult.Ok(registration.MaHoiVien)
            : EligibilityResult.Fail("Hội viên đang ngừng hoạt động.");
    }

    private sealed record EligibilityResult(bool Succeeded, int? MemberId, string? Error)
    {
        public static EligibilityResult Ok(int memberId) => new(true, memberId, null);
        public static EligibilityResult Fail(string error) => new(false, null, error);
    }
}

