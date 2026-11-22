using FitGym.Application.Common;
using FitGym.Application.Features.Payments;
using FitGym.Application.Features.Registrations;
using FitGym.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitGym.Infrastructure.Features.Payments;

public sealed class MembershipPaymentQuery(FitGymDbContext db) : IMembershipPaymentQuery
{
    private const int PageSize = 50;

    private sealed class PaymentRow
    {
        public int PaymentId { get; init; }
        public int RegistrationId { get; init; }
        public int MemberId { get; init; }
        public string MemberName { get; init; } = "";
        public string PackageName { get; init; } = "";
        public string EmployeeName { get; init; } = "";
        public DateTime RecordedAtVietnam { get; init; }
        public decimal Amount { get; init; }
        public string Method { get; init; } = "";
        public string Status { get; init; } = "";
        public string RequestCode { get; init; } = "";
    }

    private static MembershipPaymentItem ToItem(PaymentRow row) => new(row.PaymentId, row.RegistrationId,
        row.MemberId, row.MemberName, row.PackageName, row.EmployeeName, row.RecordedAtVietnam,
        row.Amount, row.Method, row.Status, row.RequestCode);

    private IQueryable<PaymentRow> Records() =>
        from payment in db.ThanhToan.AsNoTracking()
        join registration in db.DangKyGoiTap.AsNoTracking() on payment.MaDangKy equals registration.MaDangKy
        join member in db.HoiVien.AsNoTracking() on registration.MaHoiVien equals member.MaHoiVien
        join package in db.GoiTap.AsNoTracking() on registration.MaGoiTap equals package.MaGoiTap
        join employeeRecord in db.NhanVien.AsNoTracking() on payment.MaNhanVien equals employeeRecord.MaNhanVien into employees
        from employee in employees.DefaultIfEmpty()
        // RegistrationService persists payment timestamps as UTC; display/filter Vietnam civil days.
        select new PaymentRow
        {
            PaymentId = payment.MaThanhToan,
            RegistrationId = registration.MaDangKy,
            MemberId = member.MaHoiVien,
            MemberName = member.HoTen,
            PackageName = package.TenGoi,
            EmployeeName = employee == null ? "Trực tuyến" : employee.HoTen,
            RecordedAtVietnam = payment.NgayThanhToan.AddHours(7),
            Amount = payment.SoTien,
            Method = payment.PhuongThuc,
            Status = payment.TrangThai,
            RequestCode = payment.MaYeuCau
        };

    public async Task<OperationResult<MembershipPaymentPage>> SearchAsync(MembershipPaymentFilter filter, CancellationToken cancellationToken = default)
    {
        if (filter.Page is < 1 or > 100000) return OperationResult<MembershipPaymentPage>.Failure("Trang tra cứu không hợp lệ.");
        if (filter.From?.Date > filter.To?.Date) return OperationResult<MembershipPaymentPage>.Failure("Ngày bắt đầu không được sau ngày kết thúc.");
        if (filter.To?.Date == DateTime.MaxValue.Date) return OperationResult<MembershipPaymentPage>.Failure("Ngày kết thúc vượt quá phạm vi hỗ trợ.");
        if (!string.IsNullOrWhiteSpace(filter.Method) && !PaymentMethods.All.Contains(filter.Method, StringComparer.Ordinal))
            return OperationResult<MembershipPaymentPage>.Failure("Phương thức thanh toán không hợp lệ.");
        if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status is not (PaymentStatuses.Pending or PaymentStatuses.Succeeded or PaymentStatuses.Failed))
            return OperationResult<MembershipPaymentPage>.Failure("Trạng thái thanh toán không hợp lệ.");
        if (filter.Search?.Length > 100) return OperationResult<MembershipPaymentPage>.Failure("Nội dung tìm kiếm tối đa 100 ký tự.");

        var query = Records();
        if (filter.From.HasValue) query = query.Where(x => x.RecordedAtVietnam >= filter.From.Value.Date);
        if (filter.To.HasValue) { var exclusiveEnd = filter.To.Value.Date.AddDays(1); query = query.Where(x => x.RecordedAtVietnam < exclusiveEnd); }
        if (!string.IsNullOrWhiteSpace(filter.Method)) query = query.Where(x => x.Method == filter.Method);
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(x => x.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            // Exact payment request or member/package text; no unpersisted banking fields.
            query = query.Where(x => x.MemberName.Contains(term) || x.PackageName.Contains(term) || x.RequestCode.Contains(term));
        }
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.RecordedAtVietnam).ThenByDescending(x => x.PaymentId)
            .Skip((filter.Page - 1) * PageSize).Take(PageSize).ToListAsync(cancellationToken);
        return OperationResult<MembershipPaymentPage>.Success(new(items.Select(ToItem).ToList(), filter.Page, PageSize, count));
    }

    public async Task<MembershipPaymentItem?> GetAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        var row = await Records().SingleOrDefaultAsync(x => x.PaymentId == paymentId, cancellationToken);
        return row is null ? null : ToItem(row);
    }
}
