using System.ComponentModel.DataAnnotations;
using FitGym.Application.Features.Members;
using FitGym.Application.Features.Packages;
using FitGym.Application.Features.Registrations;

namespace FitGym.Web.Features.Registrations;

public sealed class RegistrationIndexViewModel
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public IReadOnlyList<RegistrationSummary> Registrations { get; init; } = [];
}

public sealed class RegistrationCreateViewModel
{
    public string? QuoteToken { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn hội viên.")]
    [Display(Name = "Hội viên")]
    public int MaHoiVien { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn gói tập.")]
    [Display(Name = "Gói tập")]
    public int MaGoiTap { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày bắt đầu")]
    public DateTime NgayBatDau { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "Phương thức thanh toán")]
    public string PhuongThucThanhToan { get; set; } = PaymentMethods.Cash;

    [Display(Name = "Đồng ý xử lý dữ liệu sinh trắc học khuôn mặt")]
    public bool DongYSinhTracHoc { get; set; }

    [Display(Name = "Đã nhận đủ tiền mặt tại quầy")]
    public bool CashReceived { get; set; }

    public IReadOnlyList<MemberSummary> Members { get; set; } = [];
    public IReadOnlyList<PackageSummary> Packages { get; set; } = [];
}

public sealed class RenewalViewModel
{
    public string? QuoteToken { get; set; }
    public int MaDangKyGoc { get; set; }
    public string HoTenHoiVien { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn gói tập.")]
    [Display(Name = "Gói gia hạn")]
    public int MaGoiTap { get; set; }

    [Required]
    [Display(Name = "Phương thức thanh toán")]
    public string PhuongThucThanhToan { get; set; } = PaymentMethods.Qr;

    [Display(Name = "Đã nhận đủ tiền mặt tại quầy")]
    public bool CashReceived { get; set; }

    public IReadOnlyList<PackageSummary> Packages { get; set; } = [];
}

internal sealed record RegistrationQuoteSnapshot(int MemberId, int PackageId, int SourceRegistrationId, RegistrationTerms Terms, DateTime ExpiresUtc, string AccountId);

