using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FitGym.Application.Features.CheckIns;
using Microsoft.AspNetCore.DataProtection;

namespace FitGym.Api.Features.Gate;

public static class GateEndpoints
{
    private sealed record AccessRequest(int RegistrationId, bool Matched, string? EventId);
    private sealed record EntryRequest(int RegistrationId, string EntryPermit, string EventId);
    private sealed record EntryPermit(int RegistrationId, DateTime ExpiresUtc);

    public static IEndpointRouteBuilder MapGateEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var gate = endpoints.MapGroup("/api/gate");
        gate.AddEndpointFilter((context, next) =>
        {
            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var configured = configuration["Gate:ApiKey"];
            if (string.IsNullOrWhiteSpace(configured) || configured.Length < 32)
                return ValueTask.FromResult<object?>(Results.StatusCode(StatusCodes.Status503ServiceUnavailable));
            var provided = context.HttpContext.Request.Headers["X-FitGym-Gate-Key"].ToString();
            var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
            var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(provided));
            if (!CryptographicOperations.FixedTimeEquals(expectedHash, providedHash))
                return ValueTask.FromResult<object?>(Results.Unauthorized());
            return next(context);
        });
        gate.MapPost("/access", AccessAsync);
        gate.MapPost("/entry", EntryAsync);
        return endpoints;
    }

    private static async Task<IResult> AccessAsync(
        AccessRequest request,
        ICheckInService checkIns,
        IDataProtectionProvider protection,
        CancellationToken cancellationToken)
    {
        if (request.RegistrationId <= 0) return Results.BadRequest(new { message = "Mã đăng ký không hợp lệ." });
        var result = request.Matched
            ? await checkIns.EvaluateGateAccessAsync(request.RegistrationId, true, cancellationToken)
            : await checkIns.RecordGateFailureAsync(request.RegistrationId, request.EventId ?? string.Empty, cancellationToken);
        if (!result.Succeeded)
            return Results.Ok(new { allowOpen = false, result.RequiresStaffAssistance, result.FailedAttempts, result.Message });
        var protector = protection.CreateProtector("FitGym.GateEntry.v1");
        var permit = protector.Protect(JsonSerializer.Serialize(
            new EntryPermit(request.RegistrationId, DateTime.UtcNow.AddMinutes(2))));
        return Results.Ok(new { allowOpen = true, entryPermit = permit });
    }

    private static async Task<IResult> EntryAsync(
        EntryRequest request,
        ICheckInService checkIns,
        IDataProtectionProvider protection,
        CancellationToken cancellationToken)
    {
        if (request.RegistrationId <= 0 || string.IsNullOrWhiteSpace(request.EntryPermit))
            return Results.BadRequest(new { message = "Thông tin mở cửa không hợp lệ." });
        EntryPermit? permit;
        try
        {
            var protector = protection.CreateProtector("FitGym.GateEntry.v1");
            permit = JsonSerializer.Deserialize<EntryPermit>(protector.Unprotect(request.EntryPermit));
        }
        catch (Exception error) when (error is CryptographicException or JsonException or FormatException)
        {
            return Results.BadRequest(new { message = "Phiên mở cửa không hợp lệ." });
        }
        if (permit is null || permit.RegistrationId != request.RegistrationId || permit.ExpiresUtc < DateTime.UtcNow)
            return Results.BadRequest(new { message = "Phiên mở cửa đã hết hạn hoặc không khớp." });
        var result = await checkIns.RecordGateEntryAsync(request.RegistrationId, request.EventId, cancellationToken);
        return result.Succeeded
            ? Results.Ok(new { checkInId = result.Value })
            : Results.BadRequest(new { message = result.Error });
    }
}
