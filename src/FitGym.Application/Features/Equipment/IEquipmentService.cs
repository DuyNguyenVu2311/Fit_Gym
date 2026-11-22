using FitGym.Application.Common;

namespace FitGym.Application.Features.Equipment;

public static class EquipmentStatuses
{
    public const string Active = "HoatDong";
    public const string UnderMaintenance = "DangBaoTri";
    public const string Retired = "NgungSuDung";
    public static readonly string[] All = [Active, UnderMaintenance, Retired];
}

public sealed record EquipmentSummary(
    int MaThietBi,
    string TenThietBi,
    string PhanLoai,
    DateTime NgayMua,
    string TrangThai,
    int SoLanBaoTri,
    DateTime? LanBaoTriGanNhat);

public sealed record MaintenanceItem(
    int MaBaoTri,
    int MaNhanVien,
    DateTime NgayBaoTri,
    string MoTa,
    decimal ChiPhi);

public sealed record EquipmentDetail(
    int MaThietBi,
    string TenThietBi,
    string PhanLoai,
    DateTime NgayMua,
    string TrangThai,
    bool DaCoLichSuBaoTri,
    IReadOnlyList<MaintenanceItem> MaintenanceHistory);

public sealed record EquipmentCreateRequest(string TenThietBi, string PhanLoai, DateTime NgayMua);
public sealed record EquipmentUpdateRequest(int MaThietBi, string TenThietBi, string PhanLoai, DateTime NgayMua, string TrangThai);
public sealed record MaintenanceCreateRequest(int MaThietBi, int MaNhanVien, DateTime NgayBaoTri, string MoTa, decimal ChiPhi);

public interface IEquipmentService
{
    Task<IReadOnlyList<EquipmentSummary>> SearchAsync(string? search, string? status, CancellationToken cancellationToken = default);
    Task<EquipmentDetail?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> CreateAsync(EquipmentCreateRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateAsync(EquipmentUpdateRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> AddMaintenanceAsync(MaintenanceCreateRequest request, CancellationToken cancellationToken = default);
}
