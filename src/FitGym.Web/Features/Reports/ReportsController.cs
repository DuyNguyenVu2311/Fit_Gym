using FitGym.Application.Features.CheckIns;
using FitGym.Application.Features.Registrations;
using FitGym.Application.Security;
using FitGym.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitGym.Web.Features.Reports;

public sealed record PackageRevenue(string PackageName, int PaymentCount, decimal Amount);

public sealed record OperationsReport(
    DateTime From,
    DateTime To,
    int PaidTransactions,
    decimal Revenue,
    int PendingQrRequests,
    int SuccessfulCheckIns,
    IReadOnlyList<PackageRevenue> Packages);

[Authorize(Roles = FitGymRoles.QuanLy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ReportsController(FitGymDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var fromDate = from?.Date ?? new DateTime(today.Year, today.Month, 1);
        var toDate = to?.Date ?? today;
        if (!ModelState.IsValid || fromDate > toDate || fromDate < DateTime.MinValue.AddDays(1) || toDate >= DateTime.MaxValue.Date)
            return BadRequest("Khoảng ngày báo cáo không hợp lệ.");

        // Payments are stored in UTC; the report uses Vietnam civil days.
        var startUtc = fromDate.AddHours(-7);
        var endUtc = toDate.AddDays(1).AddHours(-7);
        var payments =
            from payment in db.ThanhToan.AsNoTracking()
            join registration in db.DangKyGoiTap.AsNoTracking() on payment.MaDangKy equals registration.MaDangKy
            where payment.NgayThanhToan >= startUtc && payment.NgayThanhToan < endUtc
            select new { Payment = payment, Registration = registration };

        var paid = payments.Where(x => x.Payment.TrangThai == PaymentStatuses.Succeeded);
        var paidCount = await paid.CountAsync(cancellationToken);
        var revenue = await paid.SumAsync(x => (decimal?)x.Payment.SoTien, cancellationToken) ?? 0m;
        var pendingQr = await payments.CountAsync(x => x.Payment.PhuongThuc == PaymentMethods.Qr && x.Payment.TrangThai == PaymentStatuses.Pending, cancellationToken);
        var packages = await (
                from row in paid
                join package in db.GoiTap.AsNoTracking() on row.Registration.MaGoiTap equals package.MaGoiTap
                group row by new { package.MaGoiTap, package.TenGoi } into grouped
                orderby grouped.Key.TenGoi
                select new PackageRevenue(grouped.Key.TenGoi, grouped.Count(), grouped.Sum(x => x.Payment.SoTien)))
            .ToListAsync(cancellationToken);
        var checkIns = await db.LichSuCheckIn.AsNoTracking().CountAsync(
            x => x.TrangThai == CheckInStatuses.Succeeded && x.ThoiGianVao >= fromDate && x.ThoiGianVao < toDate.AddDays(1),
            cancellationToken);

        return View(new OperationsReport(fromDate, toDate, paidCount, revenue, pendingQr, checkIns, packages));
    }
}
