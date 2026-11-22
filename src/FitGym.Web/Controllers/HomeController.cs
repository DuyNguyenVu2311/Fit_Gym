using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FitGym.Web.Models;
using Microsoft.AspNetCore.Authorization;
using FitGym.Application.Features.CheckIns;
using FitGym.Application.Features.Registrations;
using FitGym.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitGym.Web.Controllers;

[Authorize(Roles = FitGym.Application.Security.FitGymRoles.AllCsv)]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ICheckInService _checkIns;
    private readonly FitGymDbContext _db;

    public HomeController(ILogger<HomeController> logger, ICheckInService checkIns, FitGymDbContext db)
    {
        _logger = logger;
        _checkIns = checkIns;
        _db = db;
    }

    public async Task<IActionResult> Index(DateTime? date, CancellationToken cancellationToken)
    {
        if (!ValidDate(date)) return BadRequest("Ngày thống kê không hợp lệ.");
        return View(await LoadAsync(date, cancellationToken));
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Snapshot(DateTime? date, CancellationToken cancellationToken)
    {
        if (!ValidDate(date)) return BadRequest("Ngày thống kê không hợp lệ.");
        return PartialView("_OperationsSnapshot", await LoadAsync(date, cancellationToken));
    }

    private bool ValidDate(DateTime? date) => ModelState.IsValid && (!date.HasValue || date.Value.Date <= DateTime.MaxValue.Date.AddDays(-4));
    private static DateTime VietnamToday => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).Date;

    private async Task<OperationsDashboardViewModel> LoadAsync(DateTime? date, CancellationToken cancellationToken)
    {
        var today = VietnamToday;
        var selected = (date ?? today).Date;
        var history = await _checkIns.GetHistoryAsync(selected, null, cancellationToken);
        var entries = history.Where(x => x.TrangThai == CheckInStatuses.Succeeded).ToList();
        var failures = history.Where(x => x.TrangThai == CheckInStatuses.FaceFailed).ToList();
        var hourly = new int[24];
        foreach (var item in entries) hourly[item.ThoiGianVao.Hour]++;
        var expiring = await _db.DangKyGoiTap.AsNoTracking().CountAsync(x =>
            x.TrangThai == RegistrationStatuses.Active && x.NgayKetThuc >= selected &&
            x.NgayKetThuc < selected.AddDays(4) &&
            _db.ThanhToan.Any(p => p.MaDangKy == x.MaDangKy && p.TrangThai == PaymentStatuses.Succeeded), cancellationToken);
        int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var accountId);
        return new OperationsDashboardViewModel
        {
            Date = selected,
            TrackToday = selected == today,
            CurrentAccountId = accountId,
            EmployeeProfileMissing = !await _db.NhanVien.AsNoTracking().AnyAsync(x => x.MaTaiKhoan == accountId, cancellationToken),
            Total = entries.Count,
            Successful = entries.Count,
            FaceFailures = failures.Count,
            Expiring = expiring,
            Hourly = hourly,
            Recent = entries.OrderByDescending(x => x.ThoiGianVao).Take(8).ToList(),
            Failed = failures.OrderByDescending(x => x.ThoiGianVao).Take(3).ToList()
        };
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
