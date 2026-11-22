using FitGym.Application.Common;
using FitGym.Application.Features.Training;
using FitGym.Application.Models;
using FitGym.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitGym.Infrastructure.Features.Training;

public sealed class TrainingFoundationService(FitGymDbContext dbContext) : ITrainingFoundationService
{
    public async Task<IReadOnlyList<MemberTrainingProfile>> ListProfilesAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query =
            from member in dbContext.HoiVien.AsNoTracking()
            join profile in dbContext.HoSoTapLuyen.AsNoTracking()
                on member.MaHoiVien equals profile.MaHoiVien into profiles
            from profile in profiles.DefaultIfEmpty()
            select new { member, profile };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.member.HoTen.Contains(term) || x.member.SoDienThoai.Contains(term));
        }

        return await query
            .OrderBy(x => x.member.HoTen)
            .Select(x => new MemberTrainingProfile(
                x.member.MaHoiVien,
                x.member.HoTen,
                x.member.TrangThai,
                x.profile == null ? null : x.profile.MaHoSo,
                x.profile == null ? null : x.profile.MucTieu,
                x.profile == null ? null : x.profile.TrinhDo,
                x.profile == null ? null : x.profile.SoNgayTuan,
                x.profile == null ? null : x.profile.SoPhutBuoi,
                x.profile == null ? null : x.profile.DieuKienTap))
            .ToListAsync(cancellationToken);
    }

    public async Task<MemberTrainingProfile?> GetProfileAsync(
        int memberId,
        CancellationToken cancellationToken = default) =>
        await (
            from member in dbContext.HoiVien.AsNoTracking()
            join profile in dbContext.HoSoTapLuyen.AsNoTracking()
                on member.MaHoiVien equals profile.MaHoiVien into profiles
            from profile in profiles.DefaultIfEmpty()
            where member.MaHoiVien == memberId
            select new MemberTrainingProfile(
                member.MaHoiVien,
                member.HoTen,
                member.TrangThai,
                profile == null ? null : profile.MaHoSo,
                profile == null ? null : profile.MucTieu,
                profile == null ? null : profile.TrinhDo,
                profile == null ? null : profile.SoNgayTuan,
                profile == null ? null : profile.SoPhutBuoi,
                profile == null ? null : profile.DieuKienTap))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<OperationResult> UpsertProfileAsync(
        TrainingProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.MucTieu))
            return OperationResult.Failure("Vui lòng nhập mục tiêu tập luyện.");
        if (string.IsNullOrWhiteSpace(request.TrinhDo))
            return OperationResult.Failure("Vui lòng nhập trình độ hiện tại.");
        if (request.SoNgayTuan is < 1 or > 7)
            return OperationResult.Failure("Số ngày tập mỗi tuần phải từ 1 đến 7.");
        if (request.SoPhutBuoi <= 0)
            return OperationResult.Failure("Thời lượng mỗi buổi phải lớn hơn 0 phút.");

        var memberExists = await dbContext.HoiVien
            .AnyAsync(x => x.MaHoiVien == request.MaHoiVien, cancellationToken);
        if (!memberExists) return OperationResult.Failure("Không tìm thấy hội viên.");

        var profile = await dbContext.HoSoTapLuyen
            .SingleOrDefaultAsync(x => x.MaHoiVien == request.MaHoiVien, cancellationToken);
        if (profile is null)
        {
            profile = new HoSoTapLuyen { MaHoiVien = request.MaHoiVien };
            dbContext.HoSoTapLuyen.Add(profile);
        }

        profile.MucTieu = request.MucTieu.Trim();
        profile.TrinhDo = request.TrinhDo.Trim();
        profile.SoNgayTuan = request.SoNgayTuan;
        profile.SoPhutBuoi = request.SoPhutBuoi;
        profile.DieuKienTap = NormalizeOptional(request.DieuKienTap);
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<IReadOnlyList<ExerciseSummary>> ListExercisesAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.BaiTap.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.TenBaiTap.Contains(term) || x.NhomCoChinh.Contains(term));
        }

        return await query
            .OrderBy(x => x.NhomCoChinh)
            .ThenBy(x => x.TenBaiTap)
            .Select(x => new ExerciseSummary(
                x.MaBaiTap, x.TenBaiTap, x.NhomCoChinh, x.MoTa, x.LinkVideo, x.HoTroAI))
            .ToListAsync(cancellationToken);
    }

    public async Task<ExerciseSummary?> GetExerciseAsync(int id, CancellationToken cancellationToken = default) =>
        await dbContext.BaiTap.AsNoTracking()
            .Where(x => x.MaBaiTap == id)
            .Select(x => new ExerciseSummary(
                x.MaBaiTap, x.TenBaiTap, x.NhomCoChinh, x.MoTa, x.LinkVideo, x.HoTroAI))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<OperationResult<int>> CreateExerciseAsync(
        ExerciseRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateExercise(request);
        if (validation is not null) return OperationResult<int>.Failure(validation);
        var name = request.TenBaiTap.Trim();
        if (await dbContext.BaiTap.AnyAsync(x => x.TenBaiTap == name, cancellationToken))
            return OperationResult<int>.Failure("Tên bài tập đã tồn tại.");

        var exercise = new BaiTap();
        Apply(exercise, request);
        dbContext.BaiTap.Add(exercise);
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Success(exercise.MaBaiTap);
    }

    public async Task<OperationResult> UpdateExerciseAsync(
        ExerciseRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateExercise(request);
        if (validation is not null) return OperationResult.Failure(validation);
        var exercise = await dbContext.BaiTap.SingleOrDefaultAsync(x => x.MaBaiTap == request.MaBaiTap, cancellationToken);
        if (exercise is null) return OperationResult.Failure("Không tìm thấy bài tập.");
        var name = request.TenBaiTap.Trim();
        if (await dbContext.BaiTap.AnyAsync(
                x => x.TenBaiTap == name && x.MaBaiTap != request.MaBaiTap,
                cancellationToken))
            return OperationResult.Failure("Tên bài tập đã tồn tại.");

        Apply(exercise, request);
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    private static string? ValidateExercise(ExerciseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TenBaiTap)) return "Vui lòng nhập tên bài tập.";
        if (string.IsNullOrWhiteSpace(request.NhomCoChinh)) return "Vui lòng nhập nhóm cơ chính.";
        if (!string.IsNullOrWhiteSpace(request.LinkVideo) &&
            (!Uri.TryCreate(request.LinkVideo.Trim(), UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            return "Liên kết video phải là địa chỉ HTTP hoặc HTTPS hợp lệ.";
        return null;
    }

    private static void Apply(BaiTap exercise, ExerciseRequest request)
    {
        exercise.TenBaiTap = request.TenBaiTap.Trim();
        exercise.NhomCoChinh = request.NhomCoChinh.Trim();
        exercise.MoTa = NormalizeOptional(request.MoTa);
        exercise.LinkVideo = NormalizeOptional(request.LinkVideo);
        exercise.HoTroAI = request.HoTroAI;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
