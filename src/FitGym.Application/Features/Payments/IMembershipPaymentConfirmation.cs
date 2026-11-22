using FitGym.Application.Common;

namespace FitGym.Application.Features.Payments;

public interface IMembershipPaymentConfirmation
{
    Task<OperationResult> ConfirmAsync(int paymentId, int employeeId, CancellationToken cancellationToken = default);
}
