using FitGym.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitGym.Web.Features.Accounts;

[Authorize(Roles = FitGymRoles.QuanLy)]
public sealed class AccountsController(AccountAdministrationService service) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        var accounts = await service.ListAsync(search);
        return View(new AccountIndexViewModel { Search = search, Accounts = accounts });
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateAccountViewModel
    {
        Role = FitGymRoles.NhanVien,
        NgayVaoLam = DateTime.Today
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAccountViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await service.CreateAsync(model);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Đã tạo tài khoản nhân viên.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await service.GetEditAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditAccountViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var currentUserName = User.Identity?.Name ?? string.Empty;
        var result = await service.UpdateAsync(model, currentUserName);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Đã cập nhật tài khoản.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var account = await service.GetEditAsync(id);
        if (account is null) return NotFound();
        return View(new ResetPasswordViewModel { UserId = id, UserName = account.UserName });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await service.ResetPasswordAsync(model.UserId, model.NewPassword);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = $"Đã đặt lại mật khẩu cho {model.UserName}.";
        return RedirectToAction(nameof(Index));
    }
}

