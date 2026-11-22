using FitGym.Application.Features.Members;
using FitGym.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitGym.Web.Features.Members;

[Authorize(Roles = FitGymRoles.AllCsv)]
public sealed class MembersController(IMemberService memberService, MemberAccountService memberAccounts) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status, CancellationToken cancellationToken)
    {
        var members = await memberService.SearchAsync(search, status, cancellationToken);
        return View(new MemberIndexViewModel { Search = search, Status = status, Members = members });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var member = await memberService.GetAsync(id, cancellationToken);
        if (member is not null) ViewData["MemberUserName"] = await memberAccounts.GetUserNameAsync(id, cancellationToken);
        return member is null ? NotFound() : View(member);
    }

    [HttpGet]
    public IActionResult Create(bool returnToRegistration = false) => View(new MemberFormViewModel
    {
        NgayThamGia = DateTime.Today,
        ReturnToRegistration = returnToRegistration
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MemberFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await memberService.CreateAsync(new MemberCreateRequest(
            model.HoTen, model.NgaySinh, model.GioiTinh, model.DiaChi,
            model.SoDienThoai, model.NgayThamGia), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Đã tạo hồ sơ hội viên.";
        if (model.ReturnToRegistration)
            return RedirectToAction("Create", "Registrations", new { memberId = result.Value });
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var member = await memberService.GetAsync(id, cancellationToken);
        if (member is null) return NotFound();
        return View(new MemberFormViewModel
        {
            MaHoiVien = member.MaHoiVien,
            HoTen = member.HoTen,
            NgaySinh = member.NgaySinh,
            GioiTinh = member.GioiTinh,
            DiaChi = member.DiaChi,
            SoDienThoai = member.SoDienThoai,
            NgayThamGia = member.NgayThamGia,
            TrangThai = member.TrangThai
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MemberFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await memberService.UpdateAsync(new MemberUpdateRequest(
            model.MaHoiVien, model.HoTen, model.NgaySinh, model.GioiTinh,
            model.DiaChi, model.SoDienThoai, model.NgayThamGia, model.TrangThai), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Đã cập nhật hội viên.";
        return RedirectToAction(nameof(Details), new { id = model.MaHoiVien });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, string status, CancellationToken cancellationToken)
    {
        var result = await memberService.SetStatusAsync(id, status, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "Đã cập nhật trạng thái hội viên."
            : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }
}

