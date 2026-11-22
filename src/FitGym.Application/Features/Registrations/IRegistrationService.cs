using FitGym.Application.Common;

namespace FitGym.Application.Features.Registrations;

public static class RegistrationStatuses
{
    public const string PendingPayment = "ChoThanhToan";
    public const string Active = "HieuLuc";
    public const string Cancelled = "DaHuy";
}

public static class PaymentStatuses
{
    public const string Pending = "ChoThanhToan";
    public const string Succeeded = "ThanhCong";
    public const string Failed = "ThatBai";
}

public static class PaymentMethods
{
    public const string Cash = "TienMat";
    public const string Qr = "QR";
    public static readonly string[] All = [Cash, Qr];
}

public sealed record RegistrationSummary(
    int MaDangKy,
    string HoTenHoiVien,
    string TenGoi,
    DateTime NgayBatDau,
    DateTime NgayKetThuc,
    decimal TongTien,
    string TrangThai,
    string TrangThaiThanhToan);

public sealed record RegistrationDetail(
    int MaDangKy,
    int? MaDangKyGoc,
    int MaHoiVien,
    int MaGoiTap,
    string HoTenHoiVien,
    string SoDienThoai,
    string TenGoi,
    DateTime NgayBatDau,
    DateTime NgayKetThuc,
    decimal TongTien,
    string TrangThai,
    string? PhuongThucThanhToan,
    string? TrangThaiThanhToan,
    string? MaYeuCauThanhToan,
    int? MaThanhToan,
    bool DaDongYSinhTracHoc,
    bool CoDuLieuKhuonMat);

public sealed record RegistrationCreateRequest(
    int MaHoiVien,
    int MaGoiTap,
    int MaNhanVien,
    DateTime NgayBatDau,
    string PhuongThucThanhToan,
    bool DongYSinhTracHoc,
    string PhienBanNoiDungDongY,
    RegistrationTerms? ConfirmedTerms = null,
    string? SubmissionKey = null);

public sealed record RenewalCreateRequest(
    int MaDangKyGoc,
    int MaGoiTap,
    int MaNhanVien,
    string PhuongThucThanhToan,
    RegistrationTerms? ConfirmedTerms = null,
    string? SubmissionKey = null);

public sealed record OnlineRenewalCreateRequest(
    int MemberId,
    int SourceRegistrationId,
    int PackageId,
    RegistrationTerms ExpectedTerms,
    string RequestKey);

public sealed record OnlineRenewalResult(int RegistrationId, int PaymentId, string PaymentRequestCode);

public interface IRegistrationService
{
    Task<IReadOnlyList<RegistrationSummary>> SearchAsync(string? search, string? status, CancellationToken cancellationToken = default);
    Task<RegistrationDetail?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<OperationResult<RegistrationTerms>> QuoteRenewalAsync(int registrationId, int packageId, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> CreateAsync(RegistrationCreateRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> RenewAsync(RenewalCreateRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<OnlineRenewalResult>> RenewOnlineAsync(OnlineRenewalCreateRequest request, CancellationToken cancellationToken = default);
}
