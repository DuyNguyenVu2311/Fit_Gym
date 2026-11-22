namespace FitGym.Api.Features.Member;

public sealed record MemberProfileDto(
    int MemberId,
    string FullName,
    DateTime? BirthDate,
    string? Gender,
    string? Address,
    string PhoneNumber,
    DateTime JoinedDate,
    string Status,
    bool HasActiveBiometricConsent,
    bool HasActiveFaceData);

public sealed record CurrentSubscriptionDto(
    int RegistrationId,
    string PackageName,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalAmount,
    string RegistrationStatus,
    string? PaymentMethod,
    string? PaymentStatus,
    string? PaymentRequestCode);

public sealed record MemberPackageDto(int PackageId, string Name, int DurationMonths, decimal Price);

public sealed record MemberRegistrationDto(
    int RegistrationId,
    int? SourceRegistrationId,
    int PackageId,
    string PackageName,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalAmount,
    string RegistrationStatus,
    string? PaymentStatus,
    string Validity);

public sealed record OnlineRenewalRequest(
    int SourceRegistrationId,
    int PackageId,
    DateTime ExpectedStartDate,
    DateTime ExpectedEndDate,
    decimal ExpectedTotalAmount,
    string RequestKey);

public sealed record MemberPaymentDto(
    int PaymentId,
    int RegistrationId,
    decimal Amount,
    DateTime PaymentDate,
    string Method,
    string Status,
    string RequestCode);

public sealed record MemberCheckInDto(
    int CheckInId,
    int RegistrationId,
    DateTime CheckedInAt,
    string Method,
    string Status);

public sealed record ApiLoginRequest(string UserNameOrEmail, string Password);

