using System.ComponentModel.DataAnnotations;

namespace FitGym.Web.Features.Accounts;

public sealed class AccountListItemViewModel
{
    public int UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string HoTen { get; init; } = string.Empty;
    public string ChucVu { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string TrangThai { get; init; } = string.Empty;
}

public sealed class AccountIndexViewModel
{
    public string? Search { get; init; }
    public IReadOnlyList<AccountListItemViewModel> Accounts { get; init; } = [];
}

public sealed class CreateAccountViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 100 ký tự.")]
    [Display(Name = "Tên đăng nhập")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu tạm thời.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu tạm thời")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên nhân viên.")]
    [StringLength(256)]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập chức vụ.")]
    [StringLength(256)]
    [Display(Name = "Chức vụ")]
    public string ChucVu { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày vào làm")]
    public DateTime NgayVaoLam { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "Vai trò")]
    public string Role { get; set; } = string.Empty;
}

public sealed class EditAccountViewModel
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên nhân viên.")]
    [StringLength(256)]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập chức vụ.")]
    [StringLength(256)]
    [Display(Name = "Chức vụ")]
    public string ChucVu { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày vào làm")]
    public DateTime? NgayVaoLam { get; set; }

    [Required]
    [Display(Name = "Vai trò")]
    public string Role { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Trạng thái")]
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class ResetPasswordViewModel
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
