using FitGym.Application.Common;
using FitGym.Application.Features.Members;
using FitGym.Application.Features.Payments;
using FitGym.Application.Features.Registrations;
using FitGym.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitGym.Infrastructure.Features.Payments;

public sealed class MembershipPaymentConfirmation(FitGymDbContext db) : IMembershipPaymentConfirmation
{
    public async Task<OperationResult> ConfirmAsync(int paymentId, int employeeId, CancellationToken cancellationToken = default)
    {
        if (!await db.NhanVien.AsNoTracking().AnyAsync(x => x.MaNhanVien == employeeId && x.TrangThai == MemberStatuses.Active, cancellationToken))
            return OperationResult.Failure("Tài khoản chưa liên kết nhân viên đang hoạt động.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var payment = await db.ThanhToan.AsNoTracking().SingleOrDefaultAsync(x => x.MaThanhToan == paymentId, cancellationToken);
        if (payment is null || payment.MaDangKy is null)
            return OperationResult.Failure("Không tìm thấy giao dịch thanh toán gói tập.");
        if (payment.PhuongThuc != PaymentMethods.Qr || payment.TrangThai != PaymentStatuses.Pending)
            return OperationResult.Failure("Giao dịch này không còn chờ xác nhận QR.");

        var registration = await db.DangKyGoiTap.AsNoTracking().SingleOrDefaultAsync(x => x.MaDangKy == payment.MaDangKy.Value, cancellationToken);
        if (registration is null || registration.TrangThai != RegistrationStatuses.PendingPayment || registration.TongTien != payment.SoTien)
            return OperationResult.Failure("Thông tin đăng ký và thanh toán không khớp. Vui lòng kiểm tra trước khi xác nhận.");
        if (await db.ThanhToan.AsNoTracking().CountAsync(x => x.MaDangKy == registration.MaDangKy, cancellationToken) != 1)
            return OperationResult.Failure("Đăng ký có nhiều giao dịch thanh toán. Vui lòng kiểm tra trước khi xác nhận.");

        var now = DateTime.UtcNow;
        var updatedPayment = await db.ThanhToan
            .Where(x => x.MaThanhToan == paymentId && x.TrangThai == PaymentStatuses.Pending && x.PhuongThuc == PaymentMethods.Qr)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.TrangThai, PaymentStatuses.Succeeded)
                .SetProperty(x => x.MaNhanVien, employeeId)
                .SetProperty(x => x.NgayThanhToan, now), cancellationToken);
        if (updatedPayment != 1)
            return OperationResult.Failure("Giao dịch đã được xử lý. Vui lòng tải lại trang.");

        var updatedRegistration = await db.DangKyGoiTap
            .Where(x => x.MaDangKy == registration.MaDangKy && x.TrangThai == RegistrationStatuses.PendingPayment)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.TrangThai, RegistrationStatuses.Active), cancellationToken);
        if (updatedRegistration != 1)
            return OperationResult.Failure("Trạng thái đăng ký đã thay đổi. Vui lòng tải lại trang.");

        await transaction.CommitAsync(cancellationToken);
        return OperationResult.Success();
    }
}
