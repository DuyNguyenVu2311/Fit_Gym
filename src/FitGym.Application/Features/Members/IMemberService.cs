using FitGym.Application.Common;

namespace FitGym.Application.Features.Members;

public static class MemberStatuses
{
    public const string Active = "HoatDong";
    public const string Inactive = "NgungHoatDong";
    public static readonly string[] All = [Active, Inactive];
}

public sealed record MemberSummary(
    int MaHoiVien,
    string HoTen,
    string SoDienThoai,
    DateTime NgayThamGia,
    string TrangThai);

public sealed record MemberDetail(
    int MaHoiVien,
    int? MaTaiKhoan,
    string HoTen,
    DateTime? NgaySinh,
    string? GioiTinh,
    string? DiaChi,
    string SoDienThoai,
    DateTime NgayThamGia,
    string TrangThai);

public sealed record MemberCreateRequest(
    string HoTen,
    DateTime? NgaySinh,
    string? GioiTinh,
    string? DiaChi,
    string SoDienThoai,
    DateTime NgayThamGia);

public sealed record MemberUpdateRequest(
    int MaHoiVien,
    string HoTen,
    DateTime? NgaySinh,
    string? GioiTinh,
    string? DiaChi,
    string SoDienThoai,
    DateTime NgayThamGia,
    string TrangThai);

public interface IMemberService
{
    Task<IReadOnlyList<MemberSummary>> SearchAsync(string? search, string? status, CancellationToken cancellationToken = default);
    Task<MemberDetail?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> CreateAsync(MemberCreateRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateAsync(MemberUpdateRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> SetStatusAsync(int id, string status, CancellationToken cancellationToken = default);
}
