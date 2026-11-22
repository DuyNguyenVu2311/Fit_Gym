using FitGym.Application.Features.Payments;
using FitGym.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System.Text;
using FitGym.Web.Features.Shared;

namespace FitGym.Web.Features.Payments;

public sealed record PaymentIndexViewModel(MembershipPaymentFilter Filter, MembershipPaymentPage Result);

[Authorize(Roles = FitGymRoles.AllCsv)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PaymentsController(IMembershipPaymentQuery query, IMembershipPaymentConfirmation confirmation, CurrentEmployeeService currentEmployeeService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateTime? from, DateTime? to, string? search, string? method, string? status, int page = 1, CancellationToken cancellationToken = default)
    {
        var filter = new MembershipPaymentFilter(from, to, search, method, status, page);
        if (!ModelState.IsValid) { Response.StatusCode = 400; return View(new PaymentIndexViewModel(filter, new([], 1, 50, 0))); }
        var result = await query.SearchAsync(filter, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            Response.StatusCode = 400;
        }
        return View(new PaymentIndexViewModel(filter, result.Value ?? new([], 1, 50, 0)));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var item = await query.GetAsync(id, cancellationToken);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, bool receiptVerified, CancellationToken cancellationToken)
    {
        var item = await query.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();
        if (!receiptVerified)
        {
            ModelState.AddModelError(string.Empty, "Vui lòng kiểm tra giao dịch và xác nhận đã nhận đủ tiền.");
            return View("Details", item);
        }

        var employeeId = await currentEmployeeService.GetEmployeeIdAsync(User, cancellationToken);
        if (!employeeId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản chưa có hồ sơ nhân viên đang hoạt động.");
            return View("Details", item);
        }

        var result = await confirmation.ConfirmAsync(id, employeeId.Value, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View("Details", await query.GetAsync(id, cancellationToken) ?? item);
        }

        TempData["Success"] = "Đã xác nhận thanh toán và cập nhật gói tập.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> QrCode(int id, CancellationToken cancellationToken)
    {
        var item = await query.GetAsync(id, cancellationToken);
        if (item is null || item.Method != "QR" || item.Status != "ChoThanhToan") return NotFound();

        var payload = PaymentQrPayload.Format(id, item.Amount, item.RequestCode);
        using var data = QRCodeGenerator.GenerateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var renderer = new SvgQRCode(data);
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = "no-store";
        return File(Encoding.UTF8.GetBytes(renderer.GetGraphic()), "image/svg+xml");
    }
}
