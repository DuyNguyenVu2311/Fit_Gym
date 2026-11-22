using FitGym.Application.Features.Registrations;

var monthEnd = RegistrationTerms.Calculate(new DateTime(2027, 1, 31, 14, 30, 0), 1, 650000m);
Ensure(monthEnd.StartDate == new DateTime(2027, 1, 31), "Start date must discard time.");
Ensure(monthEnd.EndDate == new DateTime(2027, 2, 28), "January 31 plus one calendar month must end on February 28.");
Ensure(monthEnd.Total == 650000m, "Quote must preserve database price without invented fees or discounts.");
var leap = RegistrationTerms.Calculate(new DateTime(2028, 1, 31), 1, 0m);
Ensure(leap.EndDate == new DateTime(2028, 2, 29), "Leap-year end date must match calendar month arithmetic.");
Ensure(leap.Total == 0m, "Zero-price package is permitted by the current model.");
Reject(() => RegistrationTerms.Calculate(DateTime.Today, 0, 1), "Zero duration");
Reject(() => RegistrationTerms.Calculate(DateTime.Today, 121, 1), "Duration beyond configured package limit");
Reject(() => RegistrationTerms.Calculate(DateTime.Today, 1, -1), "Negative amount");
Reject(() => RegistrationTerms.Calculate(default, 1, 1), "Missing date");
Reject(() => RegistrationTerms.Calculate(DateTime.MaxValue, 1, 1), "Overflowing end date");
Ensure(RegistrationValidity.Evaluate(RegistrationStatuses.Active, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1), false, DateTime.Today) == RegistrationValidity.PaymentUnconfirmed,
    "An active database status without a successful payment must not be shown as usable.");
Ensure(RegistrationValidity.Evaluate(RegistrationStatuses.Active, DateTime.Today.AddDays(1), DateTime.Today.AddDays(2), true, DateTime.Today) == RegistrationValidity.NotStarted,
    "A paid future registration must not be shown as currently usable.");
Ensure(RegistrationValidity.Evaluate(RegistrationStatuses.Active, DateTime.Today.AddDays(-2), DateTime.Today.AddDays(-1), true, DateTime.Today) == RegistrationValidity.Expired,
    "An expired paid registration must be shown as expired.");
Ensure(RegistrationValidity.Evaluate(RegistrationStatuses.Active, DateTime.Today.AddDays(-1), DateTime.Today, true, DateTime.Today) == RegistrationValidity.Usable,
    "The displayed final day must still be usable.");
Console.WriteLine("registration terms behavioral tests: PASS");

static void Ensure(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
static void Reject(Action action, string scenario)
{
    try { action(); }
    catch (ArgumentException) { return; }
    throw new InvalidOperationException(scenario + " must be rejected.");
}
