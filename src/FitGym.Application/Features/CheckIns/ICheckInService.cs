using FitGym.Application.Common;

namespace FitGym.Application.Features.CheckIns;

public static class CheckInMethods
{
    public const string Face = "KhuonMat";
    public const string Manual = "ThuCong";
}

public static class CheckInStatuses
{
    public const string Succeeded = "ThanhCong";
    public const string FaceFailed = "NhanDienThatBai";
}

public sealed record CheckInCandidate(
    int MaDangKy,
    int MaHoiVien,
    string HoTen,
    string SoDienThoai,
    string TenGoi,
    DateTime NgayKetThuc);

public sealed record CheckInHistoryItem(
    int MaCheckIn,
    int MaDangKy,
    string HoTen,
    string TenGoi,
    DateTime ThoiGianVao,
    string PhuongThuc,
    string TrangThai);

public sealed record CheckInResult(
    bool Succeeded,
    bool RequiresStaffAssistance,
    int FailedAttempts,
    int? CheckInId,
    string Message);

public interface ICheckInService
{
    Task<IReadOnlyList<CheckInCandidate>> GetEligibleCandidatesAsync(string? search, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CheckInHistoryItem>> GetHistoryAsync(DateTime date, string? search, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> RecordManualAsync(int registrationId, string assistanceReason, CancellationToken cancellationToken = default);
    Task<CheckInResult> RecordFaceAttemptAsync(int registrationId, bool matched, CancellationToken cancellationToken = default);
    Task<CheckInResult> EvaluateGateAccessAsync(int registrationId, bool matched, CancellationToken cancellationToken = default);
    Task<CheckInResult> RecordGateFailureAsync(int registrationId, string eventId, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> RecordGateEntryAsync(int registrationId, string eventId, CancellationToken cancellationToken = default);
}
