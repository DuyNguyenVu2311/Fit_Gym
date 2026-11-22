using System.Security.Claims;
using FitGym.Infrastructure.Persistence;
using FitGym.Infrastructure.Identity;
using FitGym.Application.Features.Registrations;
using FitGym.Application.Features.Members;
using FitGym.Application.Features.Packages;
using FitGym.Application.Features.Payments;
using QRCoder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitGym.Api.Features.Member;

public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/login", LoginAsync).AllowAnonymous();

        var memberApi = endpoints.MapGroup(string.Empty).RequireAuthorization();
        memberApi.AddEndpointFilter(async (context, next) =>
        {
            var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
            return await AccountSession.IsCurrentAsync(context.HttpContext.User, users) ? await next(context) : Results.Unauthorized();
        });
        memberApi.MapGet("/api/member/me", GetProfileAsync);
        memberApi.MapGet("/api/member/subscriptions/current", GetCurrentSubscriptionAsync);
        memberApi.MapGet("/api/member/subscriptions", GetSubscriptionsAsync);
        memberApi.MapGet("/api/member/packages", GetPackagesAsync);
        memberApi.MapGet("/api/member/renewals/quote", GetRenewalQuoteAsync);
        memberApi.MapPost("/api/member/renewals", CreateRenewalAsync);
        memberApi.MapGet("/api/member/payments", GetPaymentsAsync);
        memberApi.MapGet("/api/member/payments/{paymentId:int}/qr", GetPaymentQrAsync);
        memberApi.MapGet("/api/member/check-ins", GetCheckInsAsync);
        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        ApiLoginRequest request,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IUserClaimsPrincipalFactory<AppUser> principalFactory)
    {
        if (string.IsNullOrWhiteSpace(request.UserNameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["credentials"] = ["Vui lòng nhập tài khoản và mật khẩu."] });

        var value = request.UserNameOrEmail.Trim();
        var user = value.Contains('@')
            ? await userManager.FindByEmailAsync(value)
            : await userManager.FindByNameAsync(value);
        if (user is null || user.TrangThai != "HoatDong") return Results.Unauthorized();
        var passwordResult = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!passwordResult.Succeeded) return Results.Unauthorized();

        var principal = await principalFactory.CreateAsync(user);
        return Results.SignIn(principal, authenticationScheme: IdentityConstants.BearerScheme);
    }

    private static async Task<IResult> GetProfileAsync(
        ClaimsPrincipal principal,
        FitGymDbContext db,
        CancellationToken cancellationToken)
    {
        var member = await ResolveMemberAsync(principal, db, cancellationToken);
        if (member is null) return Results.NotFound(new { message = "Tài khoản chưa liên kết hồ sơ hội viên." });
        var hasConsent = await db.DongYSinhTracHoc.AsNoTracking()
            .AnyAsync(x => x.MaHoiVien == member.MaHoiVien && x.NgayRutLai == null, cancellationToken);
        var hasFaceData = await db.DuLieuKhuonMat.AsNoTracking()
            .AnyAsync(x => x.MaHoiVien == member.MaHoiVien && x.DangHoatDong, cancellationToken);
        return Results.Ok(new MemberProfileDto(
            member.MaHoiVien, member.HoTen, member.NgaySinh, member.GioiTinh,
            member.DiaChi, member.SoDienThoai, member.NgayThamGia,
            member.TrangThai, hasConsent, hasFaceData));
    }

    private static async Task<IResult> GetCurrentSubscriptionAsync(
        ClaimsPrincipal principal,
        FitGymDbContext db,
        CancellationToken cancellationToken)
    {
        var member = await ResolveMemberAsync(principal, db, cancellationToken);
        if (member is null) return Results.NotFound(new { message = "Tài khoản chưa liên kết hồ sơ hội viên." });
        var today = DateTime.Today;
        var subscription = await (
            from registration in db.DangKyGoiTap.AsNoTracking()
            join package in db.GoiTap.AsNoTracking() on registration.MaGoiTap equals package.MaGoiTap
            join payment in db.ThanhToan.AsNoTracking() on registration.MaDangKy equals payment.MaDangKy into payments
            from payment in payments.Where(x => x.TrangThai == PaymentStatuses.Succeeded).OrderByDescending(x => x.MaThanhToan).Take(1).DefaultIfEmpty()
            where registration.MaHoiVien == member.MaHoiVien && registration.TrangThai == RegistrationStatuses.Active
                && registration.NgayBatDau <= today && registration.NgayKetThuc >= today
                && payment != null && payment.TrangThai == PaymentStatuses.Succeeded
            orderby registration.NgayKetThuc descending, registration.MaDangKy descending
            select new CurrentSubscriptionDto(
                registration.MaDangKy,
                package.TenGoi,
                registration.NgayBatDau,
                registration.NgayKetThuc,
                registration.TongTien,
                registration.TrangThai,
                payment == null ? null : payment.PhuongThuc,
                payment == null ? null : payment.TrangThai,
                payment == null ? null : payment.MaYeuCau))
            .FirstOrDefaultAsync(cancellationToken);
        return subscription is null ? Results.NoContent() : Results.Ok(subscription);
    }

    private static async Task<IResult> GetSubscriptionsAsync(
        ClaimsPrincipal principal,
        FitGymDbContext db,
        CancellationToken cancellationToken)
    {
        var member = await ResolveMemberAsync(principal, db, cancellationToken);
        if (member is null) return Results.NotFound();
        var rows = await (
            from registration in db.DangKyGoiTap.AsNoTracking()
            join package in db.GoiTap.AsNoTracking() on registration.MaGoiTap equals package.MaGoiTap
            join payment in db.ThanhToan.AsNoTracking() on registration.MaDangKy equals payment.MaDangKy into payments
            from payment in payments.OrderByDescending(x => x.MaThanhToan).Take(1).DefaultIfEmpty()
            where registration.MaHoiVien == member.MaHoiVien
            orderby registration.MaDangKy descending
            select new
            {
                registration.MaDangKy, registration.MaDangKyGoc, registration.MaGoiTap, package.TenGoi,
                registration.NgayBatDau, registration.NgayKetThuc, registration.TongTien,
                registration.TrangThai, PaymentStatus = payment == null ? null : payment.TrangThai
            }).ToListAsync(cancellationToken);
        var today = DateTime.Today;
        return Results.Ok(rows.Select(x => new MemberRegistrationDto(
            x.MaDangKy, x.MaDangKyGoc, x.MaGoiTap, x.TenGoi, x.NgayBatDau, x.NgayKetThuc,
            x.TongTien, x.TrangThai, x.PaymentStatus,
            RegistrationValidity.Evaluate(x.TrangThai, x.NgayBatDau, x.NgayKetThuc,
                x.PaymentStatus == PaymentStatuses.Succeeded, today))));
    }

    private static async Task<IResult> GetPackagesAsync(FitGymDbContext db, CancellationToken cancellationToken)
    {
        var packages = await db.GoiTap.AsNoTracking()
            .Where(x => x.TrangThai == PackageStatuses.Active)
            .OrderBy(x => x.GiaTien).ThenBy(x => x.MaGoiTap)
            .Select(x => new MemberPackageDto(x.MaGoiTap, x.TenGoi, x.ThoiHanThang, x.GiaTien))
            .ToListAsync(cancellationToken);
        return Results.Ok(packages);
    }

    private static async Task<IResult> GetRenewalQuoteAsync(
        ClaimsPrincipal principal,
        FitGymDbContext db,
        IRegistrationService registrations,
        int registrationId,
        int packageId,
        CancellationToken cancellationToken)
    {
        var member = await ResolveMemberAsync(principal, db, cancellationToken);
        if (member is null) return Results.NotFound();
        if (!await db.DangKyGoiTap.AsNoTracking().AnyAsync(
                x => x.MaDangKy == registrationId && x.MaHoiVien == member.MaHoiVien, cancellationToken))
            return Results.NotFound();
        var quote = await registrations.QuoteRenewalAsync(registrationId, packageId, cancellationToken);
        return quote.Succeeded
            ? Results.Ok(new { startDate = quote.Value!.StartDate, endDate = quote.Value.EndDate, totalAmount = quote.Value.Total })
            : Results.BadRequest(new { message = quote.Error });
    }

    private static async Task<IResult> CreateRenewalAsync(
        ClaimsPrincipal principal,
        FitGymDbContext db,
        IRegistrationService registrations,
        OnlineRenewalRequest request,
        CancellationToken cancellationToken)
    {
        var member = await ResolveMemberAsync(principal, db, cancellationToken);
        if (member is null) return Results.NotFound();
        if (request.SourceRegistrationId <= 0 || request.PackageId <= 0 || request.ExpectedStartDate == default ||
            request.ExpectedEndDate == default || request.ExpectedTotalAmount < 0 || string.IsNullOrWhiteSpace(request.RequestKey))
            return Results.BadRequest(new { message = "Thông tin gia hạn không hợp lệ." });
        var result = await registrations.RenewOnlineAsync(new OnlineRenewalCreateRequest(
            member.MaHoiVien, request.SourceRegistrationId, request.PackageId,
            new RegistrationTerms(request.ExpectedStartDate.Date, request.ExpectedEndDate.Date, request.ExpectedTotalAmount),
            request.RequestKey), cancellationToken);
        return result.Succeeded
            ? Results.Ok(new { registrationId = result.Value!.RegistrationId, paymentId = result.Value.PaymentId, paymentRequestCode = result.Value.PaymentRequestCode })
            : Results.BadRequest(new { message = result.Error });
    }

    private static async Task<IResult> GetPaymentsAsync(
        ClaimsPrincipal principal,
        FitGymDbContext db,
        CancellationToken cancellationToken)
    {
        var member = await ResolveMemberAsync(principal, db, cancellationToken);
        if (member is null) return Results.NotFound(new { message = "Tài khoản chưa liên kết hồ sơ hội viên." });
        var payments = await (
            from payment in db.ThanhToan.AsNoTracking()
            join registration in db.DangKyGoiTap.AsNoTracking() on payment.MaDangKy equals registration.MaDangKy
            where registration.MaHoiVien == member.MaHoiVien
            orderby payment.NgayThanhToan descending, payment.MaThanhToan descending
            select new MemberPaymentDto(
                payment.MaThanhToan,
                registration.MaDangKy,
                payment.SoTien,
                payment.NgayThanhToan,
                payment.PhuongThuc,
                payment.TrangThai,
                payment.MaYeuCau))
            .ToListAsync(cancellationToken);
        return Results.Ok(payments);
    }

    private static async Task<IResult> GetPaymentQrAsync(
        ClaimsPrincipal principal,
        FitGymDbContext db,
        HttpContext httpContext,
        int paymentId,
        CancellationToken cancellationToken)
    {
        var member = await ResolveMemberAsync(principal, db, cancellationToken);
        if (member is null) return Results.NotFound();
        var payment = await (
            from item in db.ThanhToan.AsNoTracking()
            join registration in db.DangKyGoiTap.AsNoTracking() on item.MaDangKy equals registration.MaDangKy
            where item.MaThanhToan == paymentId && registration.MaHoiVien == member.MaHoiVien &&
                  item.PhuongThuc == PaymentMethods.Qr && item.TrangThai == PaymentStatuses.Pending
            select new { item.MaThanhToan, item.SoTien, item.MaYeuCau }
        ).SingleOrDefaultAsync(cancellationToken);
        if (payment is null) return Results.NotFound();
        using var qr = QRCodeGenerator.GenerateQrCode(
            PaymentQrPayload.Format(payment.MaThanhToan, payment.SoTien, payment.MaYeuCau),
            QRCodeGenerator.ECCLevel.Q);
        using var png = new PngByteQRCode(qr);
        httpContext.Response.Headers.CacheControl = "no-store";
        httpContext.Response.Headers.XContentTypeOptions = "nosniff";
        return Results.File(png.GetGraphic(8), "image/png");
    }

    private static async Task<IResult> GetCheckInsAsync(
        ClaimsPrincipal principal,
        FitGymDbContext db,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        var member = await ResolveMemberAsync(principal, db, cancellationToken);
        if (member is null) return Results.NotFound(new { message = "Tài khoản chưa liên kết hồ sơ hội viên." });
        var end = (to ?? DateTime.Today).Date.AddDays(1);
        var start = (from ?? end.AddDays(-30)).Date;
        if (end <= start || (end - start).TotalDays > 366)
            return Results.BadRequest(new { message = "Khoảng thời gian tra cứu không hợp lệ." });
        var history = await (
            from checkIn in db.LichSuCheckIn.AsNoTracking()
            join registration in db.DangKyGoiTap.AsNoTracking() on checkIn.MaDangKy equals registration.MaDangKy
            where registration.MaHoiVien == member.MaHoiVien &&
                  checkIn.TrangThai == "ThanhCong" &&
                  checkIn.ThoiGianVao >= start && checkIn.ThoiGianVao < end
            orderby checkIn.ThoiGianVao descending
            select new MemberCheckInDto(
                checkIn.MaCheckIn,
                registration.MaDangKy,
                checkIn.ThoiGianVao,
                checkIn.PhuongThuc,
                checkIn.TrangThai))
            .ToListAsync(cancellationToken);
        return Results.Ok(history);
    }

    private static async Task<FitGym.Application.Models.HoiVien?> ResolveMemberAsync(
        ClaimsPrincipal principal,
        FitGymDbContext db,
        CancellationToken cancellationToken)
    {
        var rawUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(rawUserId, out var userId)) return null;
        return await db.HoiVien.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaTaiKhoan == userId, cancellationToken);
    }
}

