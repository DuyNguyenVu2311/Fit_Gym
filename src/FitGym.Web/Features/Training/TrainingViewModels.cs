using System.ComponentModel.DataAnnotations;
using FitGym.Application.Features.Training;

namespace FitGym.Web.Features.Training;

public sealed class TrainingProfileIndexViewModel
{
    public string? Search { get; init; }
    public IReadOnlyList<MemberTrainingProfile> Profiles { get; init; } = [];
}

public sealed class TrainingProfileFormViewModel
{
    public int MaHoiVien { get; set; }
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mục tiêu tập luyện.")]
    [StringLength(256)]
    [Display(Name = "Mục tiêu")]
    public string MucTieu { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập trình độ hiện tại.")]
    [StringLength(100)]
    [Display(Name = "Trình độ")]
    public string TrinhDo { get; set; } = string.Empty;

    [Range(1, 7, ErrorMessage = "Số ngày mỗi tuần phải từ 1 đến 7.")]
    [Display(Name = "Số ngày mỗi tuần")]
    public int SoNgayTuan { get; set; } = 3;

    [Range(1, 600, ErrorMessage = "Thời lượng buổi tập phải từ 1 đến 600 phút.")]
    [Display(Name = "Số phút mỗi buổi")]
    public int SoPhutBuoi { get; set; } = 60;

    [StringLength(256)]
    [Display(Name = "Điều kiện hoặc hạn chế tập")]
    public string? DieuKienTap { get; set; }
}

public sealed class ExerciseIndexViewModel
{
    public string? Search { get; init; }
    public IReadOnlyList<ExerciseSummary> Exercises { get; init; } = [];
}

public sealed class ExerciseFormViewModel
{
    public int MaBaiTap { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên bài tập.")]
    [StringLength(256)]
    [Display(Name = "Tên bài tập")]
    public string TenBaiTap { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập nhóm cơ chính.")]
    [StringLength(256)]
    [Display(Name = "Nhóm cơ chính")]
    public string NhomCoChinh { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Mô tả kỹ thuật")]
    public string? MoTa { get; set; }

    [Url(ErrorMessage = "Liên kết video không hợp lệ.")]
    [StringLength(1000)]
    [Display(Name = "Liên kết video hướng dẫn")]
    public string? LinkVideo { get; set; }

    [Display(Name = "Đã có mô hình AI hỗ trợ")]
    public bool HoTroAI { get; set; }
}
