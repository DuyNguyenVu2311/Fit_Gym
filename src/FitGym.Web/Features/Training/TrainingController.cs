using FitGym.Application.Features.Training;
using FitGym.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitGym.Web.Features.Training;

[Authorize(Roles = FitGymRoles.AllCsv)]
public sealed class TrainingController(ITrainingFoundationService service) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Profiles(string? search, CancellationToken cancellationToken)
    {
        var profiles = await service.ListProfilesAsync(search, cancellationToken);
        return View(new TrainingProfileIndexViewModel { Search = search, Profiles = profiles });
    }

    [HttpGet]
    public async Task<IActionResult> EditProfile(int memberId, CancellationToken cancellationToken)
    {
        var profile = await service.GetProfileAsync(memberId, cancellationToken);
        if (profile is null) return NotFound();
        return View(new TrainingProfileFormViewModel
        {
            MaHoiVien = profile.MaHoiVien,
            HoTen = profile.HoTen,
            MucTieu = profile.MucTieu ?? string.Empty,
            TrinhDo = profile.TrinhDo ?? string.Empty,
            SoNgayTuan = profile.SoNgayTuan ?? 3,
            SoPhutBuoi = profile.SoPhutBuoi ?? 60,
            DieuKienTap = profile.DieuKienTap
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(TrainingProfileFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await service.UpsertProfileAsync(new TrainingProfileRequest(
            model.MaHoiVien, model.MucTieu, model.TrinhDo,
            model.SoNgayTuan, model.SoPhutBuoi, model.DieuKienTap), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }
        TempData["Success"] = "Đã cập nhật hồ sơ tập luyện.";
        return RedirectToAction(nameof(Profiles));
    }

    [HttpGet]
    public async Task<IActionResult> Exercises(string? search, CancellationToken cancellationToken)
    {
        var exercises = await service.ListExercisesAsync(search, cancellationToken);
        return View(new ExerciseIndexViewModel { Search = search, Exercises = exercises });
    }

    [HttpGet]
    [Authorize(Roles = FitGymRoles.QuanLy)]
    public IActionResult CreateExercise() => View("ExerciseForm", new ExerciseFormViewModel());

    [HttpPost]
    [Authorize(Roles = FitGymRoles.QuanLy)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateExercise(ExerciseFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View("ExerciseForm", model);
        var result = await service.CreateExerciseAsync(ToRequest(model), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View("ExerciseForm", model);
        }
        TempData["Success"] = "Đã thêm bài tập.";
        return RedirectToAction(nameof(Exercises));
    }

    [HttpGet]
    [Authorize(Roles = FitGymRoles.QuanLy)]
    public async Task<IActionResult> EditExercise(int id, CancellationToken cancellationToken)
    {
        var exercise = await service.GetExerciseAsync(id, cancellationToken);
        if (exercise is null) return NotFound();
        return View("ExerciseForm", new ExerciseFormViewModel
        {
            MaBaiTap = exercise.MaBaiTap,
            TenBaiTap = exercise.TenBaiTap,
            NhomCoChinh = exercise.NhomCoChinh,
            MoTa = exercise.MoTa,
            LinkVideo = exercise.LinkVideo,
            HoTroAI = exercise.HoTroAI
        });
    }

    [HttpPost]
    [Authorize(Roles = FitGymRoles.QuanLy)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditExercise(ExerciseFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View("ExerciseForm", model);
        var result = await service.UpdateExerciseAsync(ToRequest(model), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View("ExerciseForm", model);
        }
        TempData["Success"] = "Đã cập nhật bài tập.";
        return RedirectToAction(nameof(Exercises));
    }

    private static ExerciseRequest ToRequest(ExerciseFormViewModel model) => new(
        model.MaBaiTap,
        model.TenBaiTap,
        model.NhomCoChinh,
        model.MoTa,
        model.LinkVideo,
        model.HoTroAI);
}
