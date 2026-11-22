using FitGym.Application.Features.Warehouse;
using FitGym.Application.Security;
using FitGym.Web.Features.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitGym.Web.Features.Warehouse;

[Authorize(Roles = FitGymRoles.AllCsv)]
public sealed class WarehouseDocumentsController(
    IWarehouseService warehouseService,
    CurrentEmployeeService currentEmployeeService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? type, string? status, CancellationToken cancellationToken)
    {
        var documents = await warehouseService.ListDocumentsAsync(type, status, cancellationToken);
        return View(new WarehouseDocumentIndexViewModel { Type = type, Status = status, Documents = documents });
    }

    [HttpGet]
    public IActionResult Create() => View(new WarehouseDocumentCreateViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WarehouseDocumentCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var employeeId = await currentEmployeeService.GetEmployeeIdAsync(User, cancellationToken);
        if (!employeeId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản chưa liên kết hồ sơ nhân viên đang hoạt động.");
            return View(model);
        }
        var result = await warehouseService.CreateDocumentAsync(
            new WarehouseDocumentCreateRequest(employeeId.Value, model.LoaiPhieu, model.GhiChu), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var model = await BuildDetailsAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLine(WarehouseLineViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await warehouseService.AddOrUpdateLineAsync(
                new WarehouseLineRequest(model.MaPhieu, model.MaHangHoa, model.SoLuong, model.DonGia), cancellationToken);
            if (result.Succeeded) return RedirectToAction(nameof(Details), new { id = model.MaPhieu });
            TempData["Error"] = result.Error;
        }
        else
        {
            TempData["Error"] = "Dòng hàng chưa hợp lệ.";
        }
        return RedirectToAction(nameof(Details), new { id = model.MaPhieu });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveLine(int id, int lineId, CancellationToken cancellationToken)
    {
        var result = await warehouseService.RemoveLineAsync(id, lineId, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Đã xóa dòng hàng." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, CancellationToken cancellationToken)
    {
        var employeeId = await currentEmployeeService.GetEmployeeIdAsync(User, cancellationToken);
        if (!employeeId.HasValue)
        {
            TempData["Error"] = "Tài khoản chưa liên kết hồ sơ nhân viên đang hoạt động.";
            return RedirectToAction(nameof(Details), new { id });
        }
        var result = await warehouseService.ConfirmAsync(id, employeeId.Value, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Đã xác nhận phiếu kho." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<WarehouseDocumentDetailsViewModel?> BuildDetailsAsync(int id, CancellationToken cancellationToken)
    {
        var document = await warehouseService.GetDocumentAsync(id, cancellationToken);
        if (document is null) return null;
        var goods = await warehouseService.SearchGoodsAsync(null, GoodsStatuses.Active, cancellationToken);
        return new WarehouseDocumentDetailsViewModel
        {
            Document = document,
            AvailableGoods = goods,
            NewLine = new WarehouseLineViewModel { MaPhieu = id }
        };
    }
}
