using FitGym.Infrastructure.Persistence;
using FitGym.Web.Models.Account;
using FitGym.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitGym.Web.Controllers;

public sealed class AccountController(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (!FitGymRoles.All.Contains(model.Role ?? string.Empty, StringComparer.Ordinal))
        {
            ModelState.AddModelError(nameof(model.Role), "Vai trò đăng nhập không hợp lệ.");
            return View(model);
        }

        var login = model.UserNameOrEmail.Trim();
        var user = await userManager.FindByNameAsync(login);
        if (user is null && login.Contains('@'))
            user = await userManager.FindByEmailAsync(login);

        if (user is null)
        {
            AddLoginError();
            return View(model);
        }

        if (user.TrangThai != "HoatDong")
        {
            ModelState.AddModelError(string.Empty, "Tài khoản hiện không hoạt động. Vui lòng liên hệ quản lý.");
            return View(model);
        }

        var passwordResult = await signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
        if (!passwordResult.Succeeded)
        {
            AddLoginError(passwordResult.IsLockedOut);
            return View(model);
        }

        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Any(FitGymRoles.All.Contains))
        {
            ModelState.AddModelError(string.Empty, "Cổng Web chỉ dành cho nhân viên và quản lý. Hội viên vui lòng sử dụng ứng dụng.");
            return View(model);
        }

        if (roles.Count != 1 || !roles.Contains(model.Role!, StringComparer.Ordinal))
        {
            AddLoginError();
            return View(model);
        }

        await signInManager.SignInAsync(user, model.RememberMe);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() => View();

    [AllowAnonymous]
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult ForgotPassword() => View();

    private void AddLoginError(bool lockedOut = false) =>
        ModelState.AddModelError(
            string.Empty,
            lockedOut
                ? "Tài khoản tạm khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau."
                : "Tên đăng nhập hoặc mật khẩu không chính xác.");
}
