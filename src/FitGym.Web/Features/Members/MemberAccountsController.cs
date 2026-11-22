using System.ComponentModel.DataAnnotations;
using FitGym.Application.Features.Members;
using FitGym.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitGym.Web.Features.Members;

public sealed class MemberAccountViewModel
{
    [Range(1, int.MaxValue)] public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 3), RegularExpression(@"[a-zA-Z0-9._-]+"), Display(Name = "Tên đăng nhập ứng dụng")]
    public string UserName { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Display(Name = "Mật khẩu mới")]
    public string Password { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp."), Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;
    public bool IsReset { get; set; }
}

[Authorize(Roles = FitGymRoles.AllCsv)]
public sealed class MemberAccountsController(IMemberService members, MemberAccountService accounts) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(int memberId, CancellationToken cancellationToken)
    {
        var member = await members.GetAsync(memberId, cancellationToken);
        if (member is null) return NotFound();
        if (member.MaTaiKhoan.HasValue) return RedirectToAction("Details", "Members", new { id = memberId });
        return View("Form", new MemberAccountViewModel { MemberId = memberId, MemberName = member.HoTen });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MemberAccountViewModel model, CancellationToken cancellationToken)
    {
        model.IsReset = false;
        return await Save(model, false, cancellationToken);
    }

    [Authorize(Roles = FitGymRoles.QuanLy), HttpGet]
    public async Task<IActionResult> ResetPassword(int memberId, CancellationToken cancellationToken)
    {
        var member = await members.GetAsync(memberId, cancellationToken);
        if (member is null || !member.MaTaiKhoan.HasValue) return NotFound();
        return View("Form", new MemberAccountViewModel { MemberId = memberId, MemberName = member.HoTen, IsReset = true });
    }

    [Authorize(Roles = FitGymRoles.QuanLy), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(MemberAccountViewModel model, CancellationToken cancellationToken)
    {
        model.IsReset = true;
        ModelState.Remove(nameof(model.UserName));
        return await Save(model, true, cancellationToken);
    }

    private async Task<IActionResult> Save(MemberAccountViewModel model, bool reset, CancellationToken cancellationToken)
    {
        var member = await members.GetAsync(model.MemberId, cancellationToken);
        if (member is null) return NotFound();
        model.MemberName = member.HoTen;
        if (!ModelState.IsValid) return View("Form", model);
        var result = reset ? await accounts.ResetPasswordAsync(model.MemberId, model.Password, cancellationToken)
            : await accounts.CreateAsync(model.MemberId, model.UserName, model.Password, cancellationToken);
        if (!result.Succeeded) { ModelState.AddModelError(string.Empty, result.Error!); return View("Form", model); }
        TempData["Success"] = reset ? "Đã đặt lại mật khẩu tài khoản hội viên." : "Đã tạo tài khoản ứng dụng. Bàn giao thông tin đăng nhập trực tiếp cho hội viên.";
        return RedirectToAction("Details", "Members", new { id = model.MemberId });
    }
}
