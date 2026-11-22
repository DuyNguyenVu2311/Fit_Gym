using System.Globalization;

namespace FitGym.Application.Features.Payments;

public static class PaymentQrPayload
{
    public static string Format(int paymentId, decimal amount, string requestCode) =>
        $"FIT_GYM|TT-{paymentId:D6}|{amount.ToString("F2", CultureInfo.InvariantCulture)}|{requestCode}";
}
