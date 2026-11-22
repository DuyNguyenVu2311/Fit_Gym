using FitGym.Application.Common;

namespace FitGym.Application.Features.Payments;

public sealed record MembershipPaymentFilter(DateTime? From, DateTime? To, string? Search, string? Method, string? Status, int Page = 1);
public sealed record MembershipPaymentItem(int PaymentId, int RegistrationId, int MemberId, string MemberName, string PackageName, string EmployeeName,
    DateTime RecordedAtVietnam, decimal Amount, string Method, string Status, string RequestCode);
public sealed record MembershipPaymentPage(IReadOnlyList<MembershipPaymentItem> Items, int Page, int PageSize, int TotalCount);

public interface IMembershipPaymentQuery
{
    Task<OperationResult<MembershipPaymentPage>> SearchAsync(MembershipPaymentFilter filter, CancellationToken cancellationToken = default);
    Task<MembershipPaymentItem?> GetAsync(int paymentId, CancellationToken cancellationToken = default);
}
