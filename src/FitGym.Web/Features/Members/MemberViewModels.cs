using System.ComponentModel.DataAnnotations;
using FitGym.Application.Features.Members;

namespace FitGym.Web.Features.Members;

public sealed class MemberIndexViewModel
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public IReadOnlyList<MemberSummary> Members { get; init; } = [];
}

public sealed class MemberFormViewModel
{
    public bool ReturnToRegistration { get; set; }
    public int MaHoiVien { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(256)]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateTime? NgaySinh { get; set; }

    [StringLength(50)]
    [Display(Name = "Giới tính")]
    public string? GioiTinh { get; set; }

    [StringLength(256)]
    [Display(Name = "Địa chỉ")]
    public string? DiaChi { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
    [StringLength(30)]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày tham gia")]
    public DateTime NgayThamGia { get; set; } = DateTime.Today;

    [Display(Name = "Trạng thái")]
    public string TrangThai { get; set; } = MemberStatuses.Active;
}

