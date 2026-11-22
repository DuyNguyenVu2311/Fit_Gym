using FitGym.Application.Common;

namespace FitGym.Application.Features.Training;

public sealed record MemberTrainingProfile(
    int MaHoiVien,
    string HoTen,
    string TrangThaiHoiVien,
    int? MaHoSo,
    string? MucTieu,
    string? TrinhDo,
    int? SoNgayTuan,
    int? SoPhutBuoi,
    string? DieuKienTap);

public sealed record TrainingProfileRequest(
    int MaHoiVien,
    string MucTieu,
    string TrinhDo,
    int SoNgayTuan,
    int SoPhutBuoi,
    string? DieuKienTap);

public sealed record ExerciseSummary(
    int MaBaiTap,
    string TenBaiTap,
    string NhomCoChinh,
    string? MoTa,
    string? LinkVideo,
    bool HoTroAI);

public sealed record ExerciseRequest(
    int MaBaiTap,
    string TenBaiTap,
    string NhomCoChinh,
    string? MoTa,
    string? LinkVideo,
    bool HoTroAI);

public interface ITrainingFoundationService
{
    Task<IReadOnlyList<MemberTrainingProfile>> ListProfilesAsync(string? search, CancellationToken cancellationToken = default);
    Task<MemberTrainingProfile?> GetProfileAsync(int memberId, CancellationToken cancellationToken = default);
    Task<OperationResult> UpsertProfileAsync(TrainingProfileRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExerciseSummary>> ListExercisesAsync(string? search, CancellationToken cancellationToken = default);
    Task<ExerciseSummary?> GetExerciseAsync(int id, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> CreateExerciseAsync(ExerciseRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateExerciseAsync(ExerciseRequest request, CancellationToken cancellationToken = default);
}
