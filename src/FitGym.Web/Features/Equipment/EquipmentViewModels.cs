using System.ComponentModel.DataAnnotations;
using FitGym.Application.Features.Equipment;

namespace FitGym.Web.Features.Equipment;

public sealed class EquipmentIndexViewModel
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public IReadOnlyList<EquipmentSummary> Equipment { get; init; } = [];
}

public sealed class EquipmentFormViewModel
{
    public int MaThietBi { get; set; }
    [Required, StringLength(256), Display(Name = "Tên thiết bị")]
    public string TenThietBi { get; set; } = string.Empty;
    [Required, StringLength(256), Display(Name = "Phân loại")]
    public string PhanLoai { get; set; } = string.Empty;
    [Required, DataType(DataType.Date), Display(Name = "Ngày mua")]
    public DateTime NgayMua { get; set; } = DateTime.Today;
    [Display(Name = "Trạng thái")]
    public string TrangThai { get; set; } = EquipmentStatuses.Active;
}

public sealed class MaintenanceFormViewModel
{
    public int MaThietBi { get; set; }
    public string TenThietBi { get; set; } = string.Empty;
    [Required, DataType(DataType.Date), Display(Name = "Ngày bảo trì")]
    public DateTime NgayBaoTri { get; set; } = DateTime.Today;
    [Required, StringLength(256), Display(Name = "Nội dung bảo trì")]
    public string MoTa { get; set; } = string.Empty;
    [Range(typeof(decimal), "0", "9999999999999999"), Display(Name = "Chi phí")]
    public decimal ChiPhi { get; set; }
}

