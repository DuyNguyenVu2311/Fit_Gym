using FitGym.Application.Features.Organization;
using FitGym.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitGym.Web.Features.Organization;

[Authorize(Roles = FitGymRoles.QuanLy)]
public sealed class OrganizationController(IOrganizationService service) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var profile = await service.GetAsync(cancellationToken);
        return View(profile is null ? new OrganizationViewModel() : new OrganizationViewModel
        {
            IsConfigured = true, HasLogo = profile.HasLogo, OrganizationName = profile.OrganizationName,
            Phone = profile.Phone, Email = profile.Email, SiteName = profile.SiteName, SiteAddress = profile.SiteAddress
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Setup(OrganizationViewModel model, CancellationToken cancellationToken)
    {
        model.IsConfigured = false;
        if (!ModelState.IsValid) return View("Index", model);
        var result = await service.SetupAsync(new(model.OrganizationName, model.Phone, model.Email, model.SiteName, model.SiteAddress), cancellationToken);
        if (!result.Succeeded) { ModelState.AddModelError(string.Empty, result.Error!); return View("Index", model); }
        TempData["Success"] = "Đã thiết lập đơn vị và cơ sở đầu tiên. Bạn có thể tiếp tục tạo gói tập và hồ sơ hội viên.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(1048576)]
    public async Task<IActionResult> Update(OrganizationViewModel model, CancellationToken cancellationToken)
    {
        var current = await service.GetAsync(cancellationToken);
        if (current is null) return RedirectToAction(nameof(Index));
        model.IsConfigured = true; model.HasLogo = current.HasLogo;
        if (model.LogoFile is { Length: > 524288 or 0 }) ModelState.AddModelError(nameof(model.LogoFile), "Logo phải có nội dung và không vượt quá 512 KiB.");
        if (!ModelState.IsValid) return View("Index", model);
        byte[]? logo = null;
        if (model.LogoFile is not null)
        {
            using var stream = new MemoryStream();
            await model.LogoFile.CopyToAsync(stream, cancellationToken);
            logo = stream.ToArray();
        }
        var result = await service.UpdateAsync(new(model.OrganizationName, model.Phone, model.Email, model.SiteName, model.SiteAddress, logo, model.RemoveLogo), cancellationToken);
        if (!result.Succeeded) { ModelState.AddModelError(string.Empty, result.Error!); return View("Index", model); }
        TempData["Success"] = "Đã cập nhật thông tin đơn vị.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Logo(CancellationToken cancellationToken)
    {
        var logo = await service.GetLogoAsync(cancellationToken);
        if (logo is null) return NotFound();
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(logo.Content, logo.ContentType);
    }
}
