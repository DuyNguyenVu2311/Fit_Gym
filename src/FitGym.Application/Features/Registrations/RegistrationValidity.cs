namespace FitGym.Application.Features.Registrations;

public static class RegistrationValidity
{
    public const string Cancelled = "Cancelled";
    public const string PaymentUnconfirmed = "PaymentUnconfirmed";
    public const string NotStarted = "NotStarted";
    public const string Expired = "Expired";
    public const string Usable = "Usable";

    public static string Evaluate(string status, DateTime startDate, DateTime endDate, bool paymentSucceeded, DateTime today)
    {
        if (status == RegistrationStatuses.Cancelled) return Cancelled;
        if (status != RegistrationStatuses.Active || !paymentSucceeded) return PaymentUnconfirmed;
        if (startDate.Date > today.Date) return NotStarted;
        if (endDate.Date < today.Date) return Expired;
        return Usable;
    }
}
