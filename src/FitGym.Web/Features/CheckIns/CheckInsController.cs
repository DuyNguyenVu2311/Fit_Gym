using FitGym.Application.Features.CheckIns;
using FitGym.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitGym.Web.Features.CheckIns;

[Authorize(Roles = FitGymRoles.AllCsv)]
public sealed class CheckInsController(ICheckInService checkInService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateTime? date, string? search, CancellationToken cancellationToken)
    {
        var selectedDate = date?.Date ?? DateTime.Today;
        var history = await checkInService.GetHistoryAsync(selectedDate, search, cancellationToken);
        return View(new CheckInIndexViewModel { Date = selectedDate, Search = search, History = history });
    }

}

