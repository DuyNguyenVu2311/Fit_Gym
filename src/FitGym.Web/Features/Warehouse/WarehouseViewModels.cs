using System.ComponentModel.DataAnnotations;
using FitGym.Application.Features.Warehouse;

namespace FitGym.Web.Features.Warehouse;

public sealed class GoodsIndexViewModel
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public IReadOnlyList<GoodsSummary> Goods { get; init; } = [];
}

public sealed class GoodsFormViewModel
{
    public int MaHangHoa { get; set; }
    [Required, StringLength(200), Display(Name = "Tên hàng hóa")]
    public string TenHang { get; set; } = string.Empty;
    [Required, StringLength(100), Display(Name = "Đơn vị tính")]
    public string DonViTinh { get; set; } = string.Empty;
    [Range(typeof(decimal), "0", "9999999999999999"), Display(Name = "Giá bán")]
    public decimal GiaBan { get; set; }
    [Display(Name = "Trạng thái")]
    public string TrangThai { get; set; } = GoodsStatuses.Active;
}

public sealed class WarehouseDocumentIndexViewModel
{
    public string? Type { get; init; }
    public string? Status { get; init; }
    public IReadOnlyList<WarehouseDocumentSummary> Documents { get; init; } = [];
}

public sealed class WarehouseDocumentCreateViewModel
{
    [Required, Display(Name = "Loại phiếu")]
    public string LoaiPhieu { get; set; } = DocumentTypes.Inbound;
    [StringLength(256), Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }
}

public sealed class WarehouseLineViewModel
{
    public int MaPhieu { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Hàng hóa")]
    public int MaHangHoa { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Số lượng")]
    public int SoLuong { get; set; } = 1;
    [Range(typeof(decimal), "0", "9999999999999999"), Display(Name = "Đơn giá")]
    public decimal DonGia { get; set; }
}

public sealed class WarehouseDocumentDetailsViewModel
{
    public required WarehouseDocumentDetail Document { get; init; }
    public IReadOnlyList<GoodsSummary> AvailableGoods { get; init; } = [];
    public WarehouseLineViewModel NewLine { get; init; } = new();
}

