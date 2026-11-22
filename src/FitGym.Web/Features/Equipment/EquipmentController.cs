using FitGym.Application.Features.Equipment;
using FitGym.Application.Security;
using FitGym.Web.Features.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitGym.Web.Features.Equipment;

[Authorize(Roles = FitGymRoles.AllCsv)]
public sealed class EquipmentController(
    IEquipmentService equipmentService,
    CurrentEmployeeService currentEmployeeService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status, CancellationToken cancellationToken)
    {
        var equipment = await equipmentService.SearchAsync(search, status, cancellationToken);
        return View(new EquipmentIndexViewModel { Search = search, Status = status, Equipment = equipment });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var equipment = await equipmentService.GetAsync(id, cancellationToken);
        return equipment is null ? NotFound() : View(equipment);
    }

    [Authorize(Roles = FitGymRoles.QuanLy)]
    [HttpGet]
    public IActionResult Create() => View(new EquipmentFormViewModel());

    [Authorize(Roles = FitGymRoles.QuanLy)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EquipmentFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await equipmentService.CreateAsync(
            new EquipmentCreateRequest(model.TenThietBi, model.PhanLoai, model.NgayMua), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }
        TempData["Success"] = "Đã tạo thiết bị.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [Authorize(Roles = FitGymRoles.QuanLy)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var equipment = await equipmentService.GetAsync(id, cancellationToken);
        if (equipment is null) return NotFound();
        return View(new EquipmentFormViewModel
        {
            MaThietBi = equipment.MaThietBi,
            TenThietBi = equipment.TenThietBi,
            PhanLoai = equipment.PhanLoai,
            NgayMua = equipment.NgayMua,
            TrangThai = equipment.TrangThai
        });
    }

    [Authorize(Roles = FitGymRoles.QuanLy)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EquipmentFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await equipmentService.UpdateAsync(new EquipmentUpdateRequest(
            model.MaThietBi, model.TenThietBi, model.PhanLoai, model.NgayMua, model.TrangThai), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }
        TempData["Success"] = "Đã cập nhật thiết bị.";
        return RedirectToAction(nameof(Details), new { id = model.MaThietBi });
    }

    [HttpGet]
    public async Task<IActionResult> AddMaintenance(int id, CancellationToken cancellationToken)
    {
        var equipment = await equipmentService.GetAsync(id, cancellationToken);
        if (equipment is null) return NotFound();
        return View(new MaintenanceFormViewModel
        {
            MaThietBi = id,
            TenThietBi = equipment.TenThietBi,
            NgayBaoTri = DateTime.Today
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMaintenance(MaintenanceFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var employeeId = await currentEmployeeService.GetEmployeeIdAsync(User, cancellationToken);
        if (!employeeId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản chưa liên kết hồ sơ nhân viên đang hoạt động.");
            return View(model);
        }
        var result = await equipmentService.AddMaintenanceAsync(new MaintenanceCreateRequest(
            model.MaThietBi, employeeId.Value, model.NgayBaoTri, model.MoTa, model.ChiPhi), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }
        TempData["Success"] = "Đã ghi nhận lịch sử bảo trì.";
        return RedirectToAction(nameof(Details), new { id = model.MaThietBi });
    }
}
