using FitGym.Application.Common;

namespace FitGym.Application.Features.Warehouse;

public static class GoodsStatuses
{
    public const string Active = "HoatDong";
    public const string Inactive = "NgungHoatDong";
    public static readonly string[] All = [Active, Inactive];
}

public static class DocumentTypes
{
    public const string Inbound = "Nhap";
    public const string Outbound = "Xuat";
    public static readonly string[] All = [Inbound, Outbound];
}

public static class DocumentStatuses
{
    public const string Draft = "BanNhap";
    public const string Confirming = "DangXacNhan";
    public const string Confirmed = "DaXacNhan";
}

public sealed record GoodsSummary(
    int MaHangHoa,
    string TenHang,
    string DonViTinh,
    decimal GiaBan,
    string TrangThai,
    int TonKho);

public sealed record GoodsDetail(
    int MaHangHoa,
    string TenHang,
    string DonViTinh,
    decimal GiaBan,
    string TrangThai,
    int TonKho,
    bool DaPhatSinhChungTu);

public sealed record GoodsCreateRequest(string TenHang, string DonViTinh, decimal GiaBan);
public sealed record GoodsUpdateRequest(int MaHangHoa, string TenHang, string DonViTinh, decimal GiaBan, string TrangThai);
public sealed record WarehouseStockItem(int MaHangHoa, string TenHang, string DonViTinh, int TonKho);

public sealed record WarehouseDocumentSummary(
    int MaPhieu,
    string LoaiPhieu,
    DateTime NgayLap,
    string TrangThai,
    string? GhiChu,
    int SoDong,
    decimal TongGiaTri);

public sealed record WarehouseDocumentLine(
    int MaChiTiet,
    int MaHangHoa,
    string TenHang,
    string DonViTinh,
    int SoLuong,
    decimal DonGia,
    decimal ThanhTien);

public sealed record WarehouseDocumentDetail(
    int MaPhieu,
    int MaNhanVien,
    string LoaiPhieu,
    DateTime NgayLap,
    string TrangThai,
    DateTime? NgayXacNhan,
    int? MaNguoiXacNhan,
    string? GhiChu,
    IReadOnlyList<WarehouseDocumentLine> Lines,
    decimal TongGiaTri);

public sealed record WarehouseDocumentCreateRequest(int MaNhanVien, string LoaiPhieu, string? GhiChu);
public sealed record WarehouseLineRequest(int MaPhieu, int MaHangHoa, int SoLuong, decimal DonGia);

public interface IWarehouseService
{
    Task<IReadOnlyList<GoodsSummary>> SearchGoodsAsync(string? search, string? status, CancellationToken cancellationToken = default);
    Task<GoodsDetail?> GetGoodsAsync(int id, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> CreateGoodsAsync(GoodsCreateRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateGoodsAsync(GoodsUpdateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseStockItem>> GetStockAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseDocumentSummary>> ListDocumentsAsync(string? type, string? status, CancellationToken cancellationToken = default);
    Task<WarehouseDocumentDetail?> GetDocumentAsync(int id, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> CreateDocumentAsync(WarehouseDocumentCreateRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> AddOrUpdateLineAsync(WarehouseLineRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> RemoveLineAsync(int documentId, int lineId, CancellationToken cancellationToken = default);
    Task<OperationResult> ConfirmAsync(int documentId, int confirmingEmployeeId, CancellationToken cancellationToken = default);
}
