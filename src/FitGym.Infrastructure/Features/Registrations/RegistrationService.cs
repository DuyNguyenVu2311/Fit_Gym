using FitGym.Application.Common;
using FitGym.Application.Features.Members;
using FitGym.Application.Features.Packages;
using FitGym.Application.Features.Registrations;
using FitGym.Application.Models;
using FitGym.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace FitGym.Infrastructure.Features.Registrations;

public sealed class RegistrationService(FitGymDbContext dbContext) : IRegistrationService
{
    public async Task<IReadOnlyList<RegistrationSummary>> SearchAsync(
        string? search,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query =
            from registration in dbContext.DangKyGoiTap.AsNoTracking()
            join member in dbContext.HoiVien.AsNoTracking() on registration.MaHoiVien equals member.MaHoiVien
            join package in dbContext.GoiTap.AsNoTracking() on registration.MaGoiTap equals package.MaGoiTap
            join payment in dbContext.ThanhToan.AsNoTracking() on registration.MaDangKy equals payment.MaDangKy into payments
            from payment in payments.OrderByDescending(x => x.MaThanhToan).Take(1).DefaultIfEmpty()
            select new { Registration = registration, Member = member, Package = package, Payment = payment };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Member.HoTen.Contains(term) || x.Member.SoDienThoai.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Registration.TrangThai == status);

        return await query
            .OrderByDescending(x => x.Registration.MaDangKy)
            .Select(x => new RegistrationSummary(
                x.Registration.MaDangKy,
                x.Member.HoTen,
                x.Package.TenGoi,
                x.Registration.NgayBatDau,
                x.Registration.NgayKetThuc,
                x.Registration.TongTien,
                x.Registration.TrangThai,
                x.Payment == null ? string.Empty : x.Payment.TrangThai))
            .ToListAsync(cancellationToken);
    }

    public async Task<RegistrationDetail?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var detail = await (
            from registration in dbContext.DangKyGoiTap.AsNoTracking()
            join member in dbContext.HoiVien.AsNoTracking() on registration.MaHoiVien equals member.MaHoiVien
            join package in dbContext.GoiTap.AsNoTracking() on registration.MaGoiTap equals package.MaGoiTap
            join payment in dbContext.ThanhToan.AsNoTracking() on registration.MaDangKy equals payment.MaDangKy into payments
            from payment in payments.OrderByDescending(x => x.MaThanhToan).Take(1).DefaultIfEmpty()
            where registration.MaDangKy == id
            select new RegistrationDetail(
                registration.MaDangKy,
                registration.MaDangKyGoc,
                member.MaHoiVien,
                package.MaGoiTap,
                member.HoTen,
                member.SoDienThoai,
                package.TenGoi,
                registration.NgayBatDau,
                registration.NgayKetThuc,
                registration.TongTien,
                registration.TrangThai,
                payment == null ? null : payment.PhuongThuc,
                payment == null ? null : payment.TrangThai,
                payment == null ? null : payment.MaYeuCau,
                payment == null ? null : payment.MaThanhToan,
                false,
                false))
            .SingleOrDefaultAsync(cancellationToken);
        if (detail is null) return null;

        var hasConsent = await dbContext.DongYSinhTracHoc.AsNoTracking()
            .AnyAsync(x => x.MaHoiVien == detail.MaHoiVien && x.NgayRutLai == null, cancellationToken);
        var hasFaceData = await dbContext.DuLieuKhuonMat.AsNoTracking()
            .AnyAsync(x => x.MaHoiVien == detail.MaHoiVien && x.DangHoatDong, cancellationToken);
        return detail with { DaDongYSinhTracHoc = hasConsent, CoDuLieuKhuonMat = hasFaceData };
    }

    public async Task<OperationResult<int>> CreateAsync(
        RegistrationCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.DongYSinhTracHoc)
            return OperationResult<int>.Failure("Cần ghi nhận đồng ý sinh trắc học trước khi thu nhận dữ liệu khuôn mặt.");
        if (string.IsNullOrWhiteSpace(request.PhienBanNoiDungDongY))
            return OperationResult<int>.Failure("Chưa cấu hình phiên bản nội dung đồng ý sinh trắc học.");
        if (request.SubmissionKey is not null && (request.SubmissionKey.Length != 64 || !request.SubmissionKey.All(Uri.IsHexDigit) || request.ConfirmedTerms is null))
            return OperationResult<int>.Failure("Mã xác nhận giao dịch không hợp lệ. Vui lòng xem lại thông tin đăng ký.");
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (request.SubmissionKey is not null)
        {
            // The existing unique request index + range lock serialize the first insert and all retries.
            var existingPayment = await dbContext.ThanhToan.FromSqlInterpolated($"SELECT * FROM THANH_TOAN WITH (UPDLOCK, HOLDLOCK) WHERE MaYeuCau = {request.SubmissionKey}")
                .SingleOrDefaultAsync(cancellationToken);
            if (existingPayment is not null)
            {
                var confirmed = request.ConfirmedTerms!;
                var existing = existingPayment.MaDangKy.HasValue
                    ? await dbContext.DangKyGoiTap.AsNoTracking().SingleOrDefaultAsync(x => x.MaDangKy == existingPayment.MaDangKy.Value, cancellationToken) : null;
                if (existing is null || existing.MaHoiVien != request.MaHoiVien || existing.MaGoiTap != request.MaGoiTap || existing.MaNhanVien != request.MaNhanVien ||
                    existing.NgayBatDau != request.NgayBatDau.Date || existing.NgayBatDau != confirmed.StartDate || existing.NgayKetThuc != confirmed.EndDate ||
                    existing.TongTien != confirmed.Total || existingPayment.PhuongThuc != request.PhuongThucThanhToan)
                    return OperationResult<int>.Failure("Yêu cầu này đã được ghi nhận với thông tin khác. Vui lòng kiểm tra giao dịch trước khi tiếp tục.");
                return OperationResult<int>.Success(existing.MaDangKy);
            }
        }
        // A committed retry returns its saved receipt above, even after catalog/member changes.
        // Current references and prices apply only when writing a new registration.
        if (request.NgayBatDau.Date < DateTime.Today)
            return OperationResult<int>.Failure("Ngày bắt đầu không thể ở quá khứ.");
        var validation = await ValidateReferencesAsync(
            request.MaHoiVien, request.MaGoiTap, request.MaNhanVien,
            request.PhuongThucThanhToan, cancellationToken);
        if (!validation.Succeeded) return OperationResult<int>.Failure(validation.Error!);
        var package = validation.Package!;
        RegistrationTerms terms;
        try { terms = RegistrationTerms.Calculate(request.NgayBatDau, package.ThoiHanThang, package.GiaTien); }
        catch (ArgumentException error) { return OperationResult<int>.Failure(error.Message); }
        if (request.ConfirmedTerms is not null && request.ConfirmedTerms != terms)
            return OperationResult<int>.Failure("Giá hoặc thời hạn đã thay đổi. Vui lòng xem lại và xác nhận thanh toán lại.");
        var registration = new DangKyGoiTap
        {
            MaHoiVien = request.MaHoiVien,
            MaGoiTap = request.MaGoiTap,
            MaNhanVien = request.MaNhanVien,
            NgayBatDau = terms.StartDate,
            NgayKetThuc = terms.EndDate,
            TongTien = terms.Total,
            TrangThai = request.PhuongThucThanhToan == PaymentMethods.Cash
                ? RegistrationStatuses.Active
                : RegistrationStatuses.PendingPayment
        };
        dbContext.DangKyGoiTap.Add(registration);

        var hasConsent = await dbContext.DongYSinhTracHoc.AnyAsync(
            x => x.MaHoiVien == request.MaHoiVien && x.NgayRutLai == null,
            cancellationToken);
        if (!hasConsent)
        {
            dbContext.DongYSinhTracHoc.Add(new DongYSinhTracHoc
            {
                MaHoiVien = request.MaHoiVien,
                NgayDongY = DateTime.UtcNow,
                PhienBanNoiDung = request.PhienBanNoiDungDongY.Trim()
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var payment = await CreatePaymentAsync(
            registration,
            request.MaNhanVien,
            request.PhuongThucThanhToan,
            cancellationToken);
        if (request.SubmissionKey is not null) payment.MaYeuCau = request.SubmissionKey;
        dbContext.ThanhToan.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return OperationResult<int>.Success(registration.MaDangKy);
    }

    public async Task<OperationResult<int>> RenewAsync(
        RenewalCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var source = await dbContext.DangKyGoiTap.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaDangKy == request.MaDangKyGoc, cancellationToken);
        if (source is null) return OperationResult<int>.Failure("Không tìm thấy đăng ký cần gia hạn.");
        if (source.TrangThai != RegistrationStatuses.Active || !await dbContext.ThanhToan.AsNoTracking()
                .AnyAsync(x => x.MaDangKy == source.MaDangKy && x.TrangThai == PaymentStatuses.Succeeded, cancellationToken))
            return OperationResult<int>.Failure("Chỉ có thể gia hạn từ đăng ký đã thanh toán.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        if (request.SubmissionKey is not null)
        {
            if (request.SubmissionKey.Length != 64 || !request.SubmissionKey.All(Uri.IsHexDigit) || request.ConfirmedTerms is null)
                return OperationResult<int>.Failure("Mã xác nhận giao dịch không hợp lệ.");
            var existingPayment = await dbContext.ThanhToan.FromSqlInterpolated($"SELECT * FROM THANH_TOAN WITH (UPDLOCK, HOLDLOCK) WHERE MaYeuCau = {request.SubmissionKey}")
                .SingleOrDefaultAsync(cancellationToken);
            if (existingPayment is not null)
            {
                var existing = existingPayment.MaDangKy.HasValue
                    ? await dbContext.DangKyGoiTap.AsNoTracking().SingleOrDefaultAsync(x => x.MaDangKy == existingPayment.MaDangKy.Value, cancellationToken)
                    : null;
                if (existing is null || existing.MaDangKyGoc != request.MaDangKyGoc || existing.MaHoiVien != source.MaHoiVien ||
                    existing.MaGoiTap != request.MaGoiTap || existing.MaNhanVien != request.MaNhanVien ||
                    existing.NgayBatDau != request.ConfirmedTerms.StartDate || existing.NgayKetThuc != request.ConfirmedTerms.EndDate ||
                    existing.TongTien != request.ConfirmedTerms.Total || existingPayment.PhuongThuc != request.PhuongThucThanhToan)
                    return OperationResult<int>.Failure("Yêu cầu gia hạn đã được sử dụng với thông tin khác.");
                return OperationResult<int>.Success(existing.MaDangKy);
            }
        }

        if (await dbContext.DangKyGoiTap.AsNoTracking().AnyAsync(
                x => x.MaHoiVien == source.MaHoiVien && x.TrangThai == RegistrationStatuses.PendingPayment,
                cancellationToken))
            return OperationResult<int>.Failure("Hội viên đang có một giao dịch chờ thanh toán.");

        var validation = await ValidateReferencesAsync(
            source.MaHoiVien, request.MaGoiTap, request.MaNhanVien,
            request.PhuongThucThanhToan, cancellationToken);
        if (!validation.Succeeded) return OperationResult<int>.Failure(validation.Error!);

        var package = validation.Package!;
        RegistrationTerms terms;
        try { terms = RegistrationTerms.Calculate(await RenewalStartAsync(source.MaHoiVien, cancellationToken), package.ThoiHanThang, package.GiaTien); }
        catch (ArgumentException error) { return OperationResult<int>.Failure(error.Message); }
        if (request.ConfirmedTerms is not null && request.ConfirmedTerms != terms)
            return OperationResult<int>.Failure("Giá hoặc thời hạn gia hạn đã thay đổi. Vui lòng xem lại và xác nhận thanh toán lại.");

        var renewal = new DangKyGoiTap
        {
            MaDangKyGoc = source.MaDangKy,
            MaHoiVien = source.MaHoiVien,
            MaGoiTap = package.MaGoiTap,
            MaNhanVien = request.MaNhanVien,
            NgayBatDau = terms.StartDate,
            NgayKetThuc = terms.EndDate,
            TongTien = terms.Total,
            TrangThai = request.PhuongThucThanhToan == PaymentMethods.Cash
                ? RegistrationStatuses.Active
                : RegistrationStatuses.PendingPayment
        };
        dbContext.DangKyGoiTap.Add(renewal);
        await dbContext.SaveChangesAsync(cancellationToken);
        var payment = await CreatePaymentAsync(renewal, request.MaNhanVien, request.PhuongThucThanhToan, cancellationToken);
        if (request.SubmissionKey is not null) payment.MaYeuCau = request.SubmissionKey;
        dbContext.ThanhToan.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return OperationResult<int>.Success(renewal.MaDangKy);
    }

    public async Task<OperationResult<OnlineRenewalResult>> RenewOnlineAsync(
        OnlineRenewalCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RequestKey) || request.RequestKey.Length != 32 || !request.RequestKey.All(Uri.IsHexDigit))
            return OperationResult<OnlineRenewalResult>.Failure("Mã yêu cầu gia hạn không hợp lệ.");
        var key = request.RequestKey.ToUpperInvariant();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var existingPayment = await dbContext.ThanhToan.FromSqlInterpolated($"SELECT * FROM THANH_TOAN WITH (UPDLOCK, HOLDLOCK) WHERE MaYeuCau = {key}")
            .SingleOrDefaultAsync(cancellationToken);
        if (existingPayment is not null)
        {
            var existing = existingPayment.MaDangKy.HasValue
                ? await dbContext.DangKyGoiTap.AsNoTracking().SingleOrDefaultAsync(x => x.MaDangKy == existingPayment.MaDangKy.Value, cancellationToken)
                : null;
            if (existing is null || existing.MaDangKyGoc != request.SourceRegistrationId || existing.MaHoiVien != request.MemberId ||
                existing.MaGoiTap != request.PackageId || existing.MaNhanVien is not null ||
                existing.NgayBatDau != request.ExpectedTerms.StartDate.Date || existing.NgayKetThuc != request.ExpectedTerms.EndDate.Date ||
                existing.TongTien != request.ExpectedTerms.Total || existingPayment.PhuongThuc != PaymentMethods.Qr)
                return OperationResult<OnlineRenewalResult>.Failure("Mã yêu cầu đã được sử dụng cho giao dịch khác.");
            return OperationResult<OnlineRenewalResult>.Success(new(existing.MaDangKy, existingPayment.MaThanhToan, key));
        }

        var source = await dbContext.DangKyGoiTap.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaDangKy == request.SourceRegistrationId && x.MaHoiVien == request.MemberId, cancellationToken);
        if (source is null || source.TrangThai != RegistrationStatuses.Active || !await dbContext.ThanhToan.AsNoTracking()
                .AnyAsync(x => x.MaDangKy == source.MaDangKy && x.TrangThai == PaymentStatuses.Succeeded, cancellationToken))
            return OperationResult<OnlineRenewalResult>.Failure("Không tìm thấy đăng ký đã thanh toán để gia hạn.");
        if (!await dbContext.HoiVien.AsNoTracking().AnyAsync(x => x.MaHoiVien == request.MemberId && x.TrangThai == MemberStatuses.Active, cancellationToken))
            return OperationResult<OnlineRenewalResult>.Failure("Hội viên không còn hoạt động.");
        if (await dbContext.DangKyGoiTap.AsNoTracking().AnyAsync(x => x.MaHoiVien == request.MemberId && x.TrangThai == RegistrationStatuses.PendingPayment, cancellationToken))
            return OperationResult<OnlineRenewalResult>.Failure("Hội viên đang có một giao dịch chờ thanh toán.");
        var package = await dbContext.GoiTap.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaGoiTap == request.PackageId && x.TrangThai == PackageStatuses.Active, cancellationToken);
        if (package is null) return OperationResult<OnlineRenewalResult>.Failure("Gói tập không còn được cung cấp.");
        RegistrationTerms terms;
        try { terms = RegistrationTerms.Calculate(await RenewalStartAsync(request.MemberId, cancellationToken), package.ThoiHanThang, package.GiaTien); }
        catch (ArgumentException error) { return OperationResult<OnlineRenewalResult>.Failure(error.Message); }
        if (request.ExpectedTerms.StartDate.Date != terms.StartDate || request.ExpectedTerms.EndDate.Date != terms.EndDate || request.ExpectedTerms.Total != terms.Total)
            return OperationResult<OnlineRenewalResult>.Failure("Giá hoặc thời hạn đã thay đổi. Vui lòng xem lại trước khi gia hạn.");

        var renewal = new DangKyGoiTap
        {
            MaDangKyGoc = source.MaDangKy,
            MaHoiVien = request.MemberId,
            MaGoiTap = request.PackageId,
            MaNhanVien = null,
            NgayBatDau = terms.StartDate,
            NgayKetThuc = terms.EndDate,
            TongTien = terms.Total,
            TrangThai = RegistrationStatuses.PendingPayment
        };
        dbContext.DangKyGoiTap.Add(renewal);
        await dbContext.SaveChangesAsync(cancellationToken);
        var payment = new ThanhToan
        {
            MaDangKy = renewal.MaDangKy,
            MaNhanVien = null,
            SoTien = terms.Total,
            NgayThanhToan = DateTime.UtcNow,
            PhuongThuc = PaymentMethods.Qr,
            TrangThai = PaymentStatuses.Pending,
            MaYeuCau = key
        };
        dbContext.ThanhToan.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return OperationResult<OnlineRenewalResult>.Success(new(renewal.MaDangKy, payment.MaThanhToan, key));
    }

    public async Task<OperationResult<RegistrationTerms>> QuoteRenewalAsync(int registrationId, int packageId, CancellationToken cancellationToken = default)
    {
        var source = await dbContext.DangKyGoiTap.AsNoTracking().SingleOrDefaultAsync(x => x.MaDangKy == registrationId, cancellationToken);
        if (source is null || source.TrangThai != RegistrationStatuses.Active || !await dbContext.ThanhToan.AsNoTracking()
                .AnyAsync(x => x.MaDangKy == registrationId && x.TrangThai == PaymentStatuses.Succeeded, cancellationToken))
            return OperationResult<RegistrationTerms>.Failure("Không tìm thấy đăng ký đã thanh toán để gia hạn.");
        var package = await dbContext.GoiTap.AsNoTracking().SingleOrDefaultAsync(x => x.MaGoiTap == packageId && x.TrangThai == PackageStatuses.Active, cancellationToken);
        if (package is null) return OperationResult<RegistrationTerms>.Failure("Gói tập không còn được cung cấp.");
        if (await dbContext.DangKyGoiTap.AnyAsync(x => x.MaHoiVien == source.MaHoiVien && x.TrangThai == RegistrationStatuses.PendingPayment, cancellationToken))
            return OperationResult<RegistrationTerms>.Failure("Hội viên đang có một giao dịch chờ thanh toán.");
        try
        {
            return OperationResult<RegistrationTerms>.Success(RegistrationTerms.Calculate(await RenewalStartAsync(source.MaHoiVien, cancellationToken), package.ThoiHanThang, package.GiaTien));
        }
        catch (ArgumentException error) { return OperationResult<RegistrationTerms>.Failure(error.Message); }
    }

    private async Task<DateTime> RenewalStartAsync(int memberId, CancellationToken cancellationToken)
    {
        var latestEnd = await dbContext.DangKyGoiTap.AsNoTracking()
            .Where(x => x.MaHoiVien == memberId && x.TrangThai == RegistrationStatuses.Active &&
                        dbContext.ThanhToan.Any(p => p.MaDangKy == x.MaDangKy && p.TrangThai == PaymentStatuses.Succeeded))
            .MaxAsync(x => (DateTime?)x.NgayKetThuc, cancellationToken);
        return latestEnd.HasValue && latestEnd.Value.Date >= DateTime.Today ? latestEnd.Value.Date.AddDays(1) : DateTime.Today;
    }

    private async Task<ThanhToan> CreatePaymentAsync(
        DangKyGoiTap registration,
        int employeeId,
        string method,
        CancellationToken cancellationToken)
    {
        string requestCode;
        do
        {
            requestCode = Guid.NewGuid().ToString("N");
        }
        while (await dbContext.ThanhToan.AnyAsync(x => x.MaYeuCau == requestCode, cancellationToken));

        return new ThanhToan
        {
            MaDangKy = registration.MaDangKy,
            MaNhanVien = employeeId,
            SoTien = registration.TongTien,
            NgayThanhToan = DateTime.UtcNow,
            PhuongThuc = method,
            TrangThai = method == PaymentMethods.Cash ? PaymentStatuses.Succeeded : PaymentStatuses.Pending,
            MaYeuCau = requestCode
        };
    }

    private async Task<ReferenceValidation> ValidateReferencesAsync(
        int memberId,
        int packageId,
        int employeeId,
        string paymentMethod,
        CancellationToken cancellationToken)
    {
        if (!PaymentMethods.All.Contains(paymentMethod, StringComparer.Ordinal))
            return ReferenceValidation.Fail("Phương thức thanh toán không hợp lệ.");
        var member = await dbContext.HoiVien.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaHoiVien == memberId, cancellationToken);
        if (member is null || member.TrangThai != MemberStatuses.Active)
            return ReferenceValidation.Fail("Hội viên không tồn tại hoặc đã ngừng hoạt động.");
        var package = await dbContext.GoiTap.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaGoiTap == packageId, cancellationToken);
        if (package is null || package.TrangThai != PackageStatuses.Active)
            return ReferenceValidation.Fail("Gói tập không tồn tại hoặc đã ngừng cung cấp.");
        var employeeExists = await dbContext.NhanVien.AsNoTracking()
            .AnyAsync(x => x.MaNhanVien == employeeId && x.TrangThai == MemberStatuses.Active, cancellationToken);
        return employeeExists
            ? ReferenceValidation.Ok(package)
            : ReferenceValidation.Fail("Tài khoản chưa liên kết nhân viên đang hoạt động.");
    }

    private sealed record ReferenceValidation(bool Succeeded, GoiTap? Package, string? Error)
    {
        public static ReferenceValidation Ok(GoiTap package) => new(true, package, null);
        public static ReferenceValidation Fail(string error) => new(false, null, error);
    }
}
