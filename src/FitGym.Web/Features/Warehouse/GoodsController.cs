using FitGym.Application.Features.Warehouse;
using FitGym.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitGym.Web.Features.Warehouse;

[Authorize(Roles = FitGymRoles.AllCsv)]
public sealed class GoodsController(IWarehouseService warehouseService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status, CancellationToken cancellationToken)
    {
        var goods = await warehouseService.SearchGoodsAsync(search, status, cancellationToken);
        return View(new GoodsIndexViewModel { Search = search, Status = status, Goods = goods });
    }

    [Authorize(Roles = FitGymRoles.QuanLy)]
    [HttpGet]
    public IActionResult Create() => View(new GoodsFormViewModel());

    [Authorize(Roles = FitGymRoles.QuanLy)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GoodsFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await warehouseService.CreateGoodsAsync(
            new GoodsCreateRequest(model.TenHang, model.DonViTinh, model.GiaBan), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }
        TempData["Success"] = "Đã tạo hàng hóa.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = FitGymRoles.QuanLy)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var goods = await warehouseService.GetGoodsAsync(id, cancellationToken);
        if (goods is null) return NotFound();
        return View(new GoodsFormViewModel
        {
            MaHangHoa = goods.MaHangHoa,
            TenHang = goods.TenHang,
            DonViTinh = goods.DonViTinh,
            GiaBan = goods.GiaBan,
            TrangThai = goods.TrangThai
        });
    }

    [Authorize(Roles = FitGymRoles.QuanLy)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(GoodsFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await warehouseService.UpdateGoodsAsync(new GoodsUpdateRequest(
            model.MaHangHoa, model.TenHang, model.DonViTinh, model.GiaBan, model.TrangThai), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }
        TempData["Success"] = "Đã cập nhật hàng hóa.";
        return RedirectToAction(nameof(Index));
    }
}

