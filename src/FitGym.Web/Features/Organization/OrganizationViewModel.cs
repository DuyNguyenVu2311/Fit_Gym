using System.ComponentModel.DataAnnotations;

namespace FitGym.Web.Features.Organization;

public sealed class OrganizationViewModel
{
    public bool IsConfigured { get; set; }
    public bool HasLogo { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tên đơn vị.")]
    [StringLength(200)]
    [Display(Name = "Tên đơn vị / thương hiệu phòng gym")]
    public string OrganizationName { get; set; } = string.Empty;
    [StringLength(30)]
    [Display(Name = "Số điện thoại liên hệ")]
    public string? Phone { get; set; }
    [EmailAddress(ErrorMessage = "Email liên hệ không hợp lệ.")]
    [StringLength(256)]
    [Display(Name = "Email liên hệ")]
    public string? Email { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tên cơ sở đầu tiên.")]
    [StringLength(200)]
    [Display(Name = "Tên cơ sở đầu tiên")]
    public string SiteName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập địa chỉ cơ sở.")]
    [StringLength(500)]
    [Display(Name = "Địa chỉ cơ sở")]
    public string SiteAddress { get; set; } = string.Empty;
    [Display(Name = "Logo đơn vị (PNG hoặc JPEG, tối đa 512 KiB)")]
    public IFormFile? LogoFile { get; set; }
    [Display(Name = "Xóa logo hiện tại")]
    public bool RemoveLogo { get; set; }
}
