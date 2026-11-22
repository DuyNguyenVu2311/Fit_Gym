using FitGym.Application.Features.Members;
using FitGym.Application.Features.Packages;
using FitGym.Application.Features.Registrations;
using FitGym.Application.Security;
using FitGym.Web.Features.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;
using System.Security.Cryptography;
using System.Security.Claims;
using Microsoft.AspNetCore.WebUtilities;

namespace FitGym.Web.Features.Registrations;

[Authorize(Roles = FitGymRoles.AllCsv)]
public sealed class RegistrationsController(
    IRegistrationService registrationService,
    IMemberService memberService,
    IPackageService packageService,
    CurrentEmployeeService currentEmployeeService,
    IDataProtectionProvider dataProtectionProvider) : Controller
{
    private const string BiometricConsentVersion = "FITGYM-BIO-CONSENT-001";
    private readonly IDataProtector _quoteProtector = dataProtectionProvider.CreateProtector("FitGym.RegistrationQuote.v1");

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Quote(int memberId, int packageId, DateTime startDate, CancellationToken cancellationToken)
    {
        var member = await memberService.GetAsync(memberId, cancellationToken);
        if (member is null || member.TrangThai != MemberStatuses.Active)
            return BadRequest(new { message = "Vui lòng chọn hội viên đang hoạt động." });
        if (startDate == default || startDate.Date < DateTime.Today)
            return BadRequest(new { message = "Vui lòng chọn ngày bắt đầu từ hôm nay trở đi." });
        var package = await packageService.GetAsync(packageId, cancellationToken);
        if (package is null || package.TrangThai != PackageStatuses.Active)
            return BadRequest(new { message = "Gói tập không còn được cung cấp. Vui lòng chọn lại." });
        try
        {
            var terms = RegistrationTerms.Calculate(startDate, package.ThoiHanThang, package.GiaTien);
            return QuoteResult(memberId, package.MaGoiTap, package.TenGoi, terms, 0);
        }
        catch (ArgumentException error) { return BadRequest(new { message = error.Message }); }
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> RenewalQuote(int registrationId, int packageId, CancellationToken cancellationToken)
    {
        var source = await registrationService.GetAsync(registrationId, cancellationToken);
        if (source is null) return NotFound(new { message = "Không tìm thấy đăng ký cần gia hạn." });
        var quote = await registrationService.QuoteRenewalAsync(registrationId, packageId, cancellationToken);
        if (!quote.Succeeded) return BadRequest(new { message = quote.Error });
        var package = await packageService.GetAsync(packageId, cancellationToken);
        return QuoteResult(source.MaHoiVien, packageId, package!.TenGoi, quote.Value!, registrationId);
    }

    private IActionResult QuoteResult(int memberId, int packageId, string name, RegistrationTerms terms, int sourceId)
    {
        var token = _quoteProtector.Protect(JsonSerializer.Serialize(new RegistrationQuoteSnapshot(memberId, packageId, sourceId, terms, DateTime.UtcNow.AddMinutes(15), User.FindFirstValue(ClaimTypes.NameIdentifier)!)));
        return Json(new { memberId, packageId, packageName = name, startDate = terms.StartDate.ToString("yyyy-MM-dd"), endDate = terms.EndDate.ToString("yyyy-MM-dd"), total = terms.Total, quoteToken = token });
    }

    private RegistrationTerms? ReadQuote(string? token, int memberId, int packageId, int sourceId, DateTime? startDate = null)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            var snapshot = JsonSerializer.Deserialize<RegistrationQuoteSnapshot>(_quoteProtector.Unprotect(token));
            if (snapshot is null || snapshot.AccountId != User.FindFirstValue(ClaimTypes.NameIdentifier) || snapshot.ExpiresUtc <= DateTime.UtcNow || snapshot.MemberId != memberId || snapshot.PackageId != packageId || snapshot.SourceRegistrationId != sourceId ||
                (startDate.HasValue && snapshot.Terms.StartDate != startDate.Value.Date)) return null;
            return snapshot.Terms;
        }
        catch (Exception error) when (error is CryptographicException or JsonException or FormatException) { return null; }
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status, CancellationToken cancellationToken)
    {
        var registrations = await registrationService.SearchAsync(search, status, cancellationToken);
        return View(new RegistrationIndexViewModel
        {
            Search = search,
            Status = status,
            Registrations = registrations
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var registration = await registrationService.GetAsync(id, cancellationToken);
        return registration is null ? NotFound() : View(registration);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? memberId, CancellationToken cancellationToken)
    {
        var model = new RegistrationCreateViewModel
        {
            MaHoiVien = memberId ?? 0,
            NgayBatDau = DateTime.Today
        };
        await PopulateCreateOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegistrationCreateViewModel model, CancellationToken cancellationToken)
    {
        var confirmedTerms = ReadQuote(model.QuoteToken, model.MaHoiVien, model.MaGoiTap, 0, model.NgayBatDau);
        if (confirmedTerms is null) ModelState.AddModelError(nameof(model.QuoteToken), "Thông tin giá/thời hạn chưa được xác nhận hoặc đã hết hạn. Vui lòng xem lại gói tập.");
        if (model.PhuongThucThanhToan == PaymentMethods.Cash && !model.CashReceived)
            ModelState.AddModelError(nameof(model.CashReceived), "Nhân viên phải xác nhận đã nhận đủ tiền mặt trước khi hoàn tất.");
        if (!ModelState.IsValid)
        {
            await PopulateCreateOptionsAsync(model, cancellationToken);
            return View(model);
        }

        var employeeId = await currentEmployeeService.GetEmployeeIdAsync(User, cancellationToken);
        if (!employeeId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản đăng nhập chưa liên kết hồ sơ nhân viên đang hoạt động.");
            await PopulateCreateOptionsAsync(model, cancellationToken);
            return View(model);
        }

        var result = await registrationService.CreateAsync(new RegistrationCreateRequest(
            model.MaHoiVien,
            model.MaGoiTap,
            employeeId.Value,
            model.NgayBatDau,
            model.PhuongThucThanhToan,
            model.DongYSinhTracHoc,
            BiometricConsentVersion,
            confirmedTerms,
            Convert.ToHexString(SHA256.HashData(WebEncoders.Base64UrlDecode(model.QuoteToken!)))), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await PopulateCreateOptionsAsync(model, cancellationToken);
            return View(model);
        }

        TempData["Success"] = model.PhuongThucThanhToan == PaymentMethods.Cash
            ? "Đăng ký và thanh toán tiền mặt đã được ghi nhận."
            : "Đã tạo yêu cầu QR. Đăng ký đang chờ thanh toán và chưa có hiệu lực.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Renew(int id, CancellationToken cancellationToken)
    {
        var source = await registrationService.GetAsync(id, cancellationToken);
        if (source is null) return NotFound();
        var model = new RenewalViewModel
        {
            MaDangKyGoc = source.MaDangKy,
            MaGoiTap = source.MaGoiTap,
            HoTenHoiVien = source.HoTenHoiVien
        };
        await PopulateRenewalOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Renew(RenewalViewModel model, CancellationToken cancellationToken)
    {
        var source = await registrationService.GetAsync(model.MaDangKyGoc, cancellationToken);
        var confirmedTerms = source is null ? null : ReadQuote(model.QuoteToken, source.MaHoiVien, model.MaGoiTap, model.MaDangKyGoc);
        if (confirmedTerms is null) ModelState.AddModelError(nameof(model.QuoteToken), "Vui lòng xem lại thời hạn và giá gia hạn trước khi thanh toán.");
        if (model.PhuongThucThanhToan == PaymentMethods.Cash && !model.CashReceived)
            ModelState.AddModelError(nameof(model.CashReceived), "Nhân viên phải xác nhận đã nhận đủ tiền mặt trước khi hoàn tất gia hạn.");
        if (!ModelState.IsValid)
        {
            await PopulateRenewalOptionsAsync(model, cancellationToken);
            return View(model);
        }

        var employeeId = await currentEmployeeService.GetEmployeeIdAsync(User, cancellationToken);
        if (!employeeId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản đăng nhập chưa liên kết hồ sơ nhân viên đang hoạt động.");
            await PopulateRenewalOptionsAsync(model, cancellationToken);
            return View(model);
        }

        var result = await registrationService.RenewAsync(new RenewalCreateRequest(
            model.MaDangKyGoc, model.MaGoiTap, employeeId.Value, model.PhuongThucThanhToan, confirmedTerms,
            Convert.ToHexString(SHA256.HashData(WebEncoders.Base64UrlDecode(model.QuoteToken!)))),
            cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await PopulateRenewalOptionsAsync(model, cancellationToken);
            return View(model);
        }

        TempData["Success"] = model.PhuongThucThanhToan == PaymentMethods.Cash
            ? "Gia hạn và thanh toán tiền mặt đã được ghi nhận."
            : "Đã tạo yêu cầu QR. Kỳ gia hạn đang chờ thanh toán và chưa có hiệu lực.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    private async Task PopulateCreateOptionsAsync(RegistrationCreateViewModel model, CancellationToken cancellationToken)
    {
        model.Members = await memberService.SearchAsync(null, MemberStatuses.Active, cancellationToken);
        model.Packages = await packageService.SearchAsync(null, PackageStatuses.Active, cancellationToken);
    }

    private async Task PopulateRenewalOptionsAsync(RenewalViewModel model, CancellationToken cancellationToken) =>
        model.Packages = await packageService.SearchAsync(null, PackageStatuses.Active, cancellationToken);
}

