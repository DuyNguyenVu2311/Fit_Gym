using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using FitGym.Application.Models;
using FitGym.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FitGym.Infrastructure;
using FitGym.Infrastructure.Features.Payments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;

const string testMarker = "__FITGYM_API_TEST__";
const string memberUserName = "fitgym_api_member_test";
const string memberPassword = "FitGym@Test2026!";
const string gateKey = "FITGYM-TEST-GATE-KEY-2026-SECURE-ONLY";
const string apiUrl = "http://127.0.0.1:5212";

var connectionString = Environment.GetEnvironmentVariable("FITGYM_TEST_CONNECTION")
    ?? "Server=.\\SQLEXPRESS;Database=FitGymDev;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
var options = new DbContextOptionsBuilder<FitGymDbContext>()
    .UseSqlServer(connectionString)
    .Options;

await using var db = new FitGymDbContext(options);
Console.WriteLine("API test: cleaning previous fixture...");
await CleanupAsync(db);

Process? apiProcess = null;
try
{
    Console.WriteLine("API test: creating fixture...");
    var passwordHasher = new PasswordHasher<AppUser>();
    var employeeUser = CreateUser("fitgym_api_employee_test", "fitgym-api-employee@test.local");
    employeeUser.PasswordHash = passwordHasher.HashPassword(employeeUser, memberPassword);
    var memberUser = CreateUser(memberUserName, "fitgym-api-member@test.local");
    memberUser.PasswordHash = passwordHasher.HashPassword(memberUser, memberPassword);
    db.Users.AddRange(employeeUser, memberUser);
    await db.SaveChangesAsync();

    var employee = new NhanVien
    {
        MaTaiKhoan = employeeUser.Id,
        HoTen = testMarker + " EMPLOYEE",
        ChucVu = "NhanVien",
        NgayVaoLam = DateTime.Today,
        TrangThai = "HoatDong"
    };
    var member = new HoiVien
    {
        MaTaiKhoan = memberUser.Id,
        HoTen = testMarker + " MEMBER",
        SoDienThoai = "0900000000",
        NgayThamGia = DateTime.Today.AddMonths(-2),
        TrangThai = "HoatDong"
    };
    var package = new GoiTap
    {
        TenGoi = testMarker + " PACKAGE",
        ThoiHanThang = 3,
        GiaTien = 900_000,
        TrangThai = "HoatDong"
    };
    db.AddRange(employee, member, package);
    await db.SaveChangesAsync();

    var registration = new DangKyGoiTap
    {
        MaHoiVien = member.MaHoiVien,
        MaGoiTap = package.MaGoiTap,
        MaNhanVien = employee.MaNhanVien,
        NgayBatDau = DateTime.Today.AddMonths(-1),
        NgayKetThuc = DateTime.Today.AddMonths(2),
        TongTien = package.GiaTien,
        TrangThai = "HieuLuc"
    };
    db.Add(registration);
    await db.SaveChangesAsync();
    db.AddRange(
        new ThanhToan
        {
            MaDangKy = registration.MaDangKy,
            MaNhanVien = employee.MaNhanVien,
            SoTien = package.GiaTien,
            NgayThanhToan = DateTime.Now.AddDays(-20),
            PhuongThuc = "TienMat",
            TrangThai = "ThanhCong",
            MaYeuCau = "API-TEST-" + Guid.NewGuid().ToString("N")
        },
        new LichSuCheckIn
        {
            MaDangKy = registration.MaDangKy,
            ThoiGianVao = DateTime.Now.AddHours(-2),
            PhuongThuc = "KhuonMat",
            TrangThai = "ThanhCong"
        },
        new LichSuCheckIn
        {
            MaDangKy = registration.MaDangKy,
            ThoiGianVao = DateTime.Now.AddHours(-1),
            PhuongThuc = "KhuonMat",
            TrangThai = "NhanDienThatBai"
        },
        new DongYSinhTracHoc
        {
            MaHoiVien = member.MaHoiVien,
            NgayDongY = DateTime.Today.AddMonths(-2),
            PhienBanNoiDung = "api-functional-test"
        },
        new DuLieuKhuonMat
        {
            MaHoiVien = member.MaHoiVien,
            VectorKhuonMat = [1, 2, 3, 4],
            PhienBanMoHinh = "api-functional-test",
            NgayTao = DateTime.UtcNow,
            DangHoatDong = true
        });
    await db.SaveChangesAsync();

    Console.WriteLine("API test: starting API...");
    var pendingRenewal = new DangKyGoiTap { MaHoiVien = member.MaHoiVien, MaGoiTap = package.MaGoiTap, MaNhanVien = employee.MaNhanVien, NgayBatDau = registration.NgayKetThuc.AddDays(1), NgayKetThuc = registration.NgayKetThuc.AddMonths(3), TongTien = package.GiaTien, TrangThai = "ChoThanhToan" };
    db.DangKyGoiTap.Add(pendingRenewal); await db.SaveChangesAsync();
    db.ThanhToan.Add(new ThanhToan { MaDangKy = pendingRenewal.MaDangKy, MaNhanVien = employee.MaNhanVien, SoTien = package.GiaTien, NgayThanhToan = DateTime.Now, PhuongThuc = "QR", TrangThai = "ChoThanhToan", MaYeuCau = "API-TEST-" + Guid.NewGuid().ToString("N") });
    await db.SaveChangesAsync();
    apiProcess = StartApi(connectionString);
    using var http = new HttpClient
    {
        BaseAddress = new Uri(apiUrl),
        Timeout = TimeSpan.FromSeconds(5)
    };
    await WaitUntilReadyAsync(http, apiProcess);
    Console.WriteLine("API test: API is ready; verifying endpoints...");

    var brandResponse = await http.GetAsync("/api/organization");
    brandResponse.EnsureSuccessStatusCode();
    using var brand = JsonDocument.Parse(await brandResponse.Content.ReadAsStringAsync());
    var allowedBrandFields = new[] { "isConfigured", "displayName", "siteName", "hasLogo" };
    Ensure(brand.RootElement.EnumerateObject().All(x => allowedBrandFields.Contains(x.Name)), "Public branding exposes only the approved public fields.");
    Ensure(brandResponse.Headers.CacheControl?.NoStore == true, "Brand changes must not be retained by shared caches.");

    var anonymous = await http.GetAsync("/api/member/me");
    Ensure((int)anonymous.StatusCode == 401, "Protected member endpoint must reject anonymous access.");

    var loginResponse = await http.PostAsJsonAsync("/api/auth/login", new
    {
        userNameOrEmail = memberUserName,
        password = memberPassword
    });
    loginResponse.EnsureSuccessStatusCode();
    using var loginJson = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
    var token = loginJson.RootElement.GetProperty("accessToken").GetString();
    Ensure(!string.IsNullOrWhiteSpace(token), "Login must return an access token.");
    http.DefaultRequestHeaders.Authorization = new("Bearer", token);

    foreach (var endpoint in new[]
             {
                 "/api/member/me",
                 "/api/member/subscriptions/current",
                 "/api/member/payments",
                 "/api/member/check-ins"
             })
    {
        var response = await http.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        Ensure(!payload.Contains("vectorKhuonMat", StringComparison.OrdinalIgnoreCase),
            $"Endpoint {endpoint} must never expose biometric vectors.");
    }

    var profile = await http.GetStringAsync("/api/member/me");
    Ensure(profile.Contains(testMarker + " MEMBER", StringComparison.Ordinal),
        "Profile endpoint must resolve the member linked to the authenticated account.");
    Ensure(profile.Contains("hasActiveFaceData", StringComparison.Ordinal),
        "Profile may expose only the presence of active face data.");

    using var entryHistory = JsonDocument.Parse(await http.GetStringAsync("/api/member/check-ins"));
    Ensure(entryHistory.RootElement.GetArrayLength() == 1 &&
        entryHistory.RootElement[0].GetProperty("status").GetString() == "ThanhCong",
        "Member entry history must exclude failed face recognition attempts.");

    var noGateKey = await http.PostAsJsonAsync("/api/gate/access", new { registrationId = registration.MaDangKy, matched = true });
    Ensure((int)noGateKey.StatusCode == 401, "Gate requests without the configured device credential must be rejected.");
    async Task<HttpResponseMessage> SendGateAsync(string path, object payload)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(payload)
        };
        message.Headers.Add("X-FitGym-Gate-Key", gateKey);
        return await http.SendAsync(message);
    }
    var gateAccess = await SendGateAsync("/api/gate/access", new { registrationId = registration.MaDangKy, matched = true });
    gateAccess.EnsureSuccessStatusCode();
    using var accessResult = JsonDocument.Parse(await gateAccess.Content.ReadAsStringAsync());
    Ensure(accessResult.RootElement.GetProperty("allowOpen").GetBoolean(), "Paid member with consent and face data must be eligible at the gate.");
    var permit = accessResult.RootElement.GetProperty("entryPermit").GetString();
    var eventId = Guid.NewGuid().ToString("N");
    var entryRequest = new { registrationId = registration.MaDangKy, entryPermit = permit, eventId };
    var entryResponse = await SendGateAsync("/api/gate/entry", entryRequest);
    entryResponse.EnsureSuccessStatusCode();
    using var entryResult = JsonDocument.Parse(await entryResponse.Content.ReadAsStringAsync());
    var newCheckInId = entryResult.RootElement.GetProperty("checkInId").GetInt32();
    var repeatedEntry = await SendGateAsync("/api/gate/entry", entryRequest);
    repeatedEntry.EnsureSuccessStatusCode();
    using var repeatedEntryResult = JsonDocument.Parse(await repeatedEntry.Content.ReadAsStringAsync());
    Ensure(repeatedEntryResult.RootElement.GetProperty("checkInId").GetInt32() == newCheckInId &&
        await db.LichSuCheckIn.AsNoTracking().CountAsync(x => x.MaDangKy == registration.MaDangKy && x.TrangThai == "ThanhCong") == 2,
        "A retried door-open event must not create a duplicate entry.");
    var failureEvent = Guid.NewGuid().ToString("N");
    var firstFailure = await SendGateAsync("/api/gate/access", new { registrationId = registration.MaDangKy, matched = false, eventId = failureEvent });
    firstFailure.EnsureSuccessStatusCode();
    var retriedFailure = await SendGateAsync("/api/gate/access", new { registrationId = registration.MaDangKy, matched = false, eventId = failureEvent });
    retriedFailure.EnsureSuccessStatusCode();
    Ensure(await db.LichSuCheckIn.AsNoTracking().CountAsync(x => x.MaSuKien == failureEvent.ToUpperInvariant()) == 1,
        "A retried failed-recognition event must count as one attempt.");
    await SendGateAsync("/api/gate/access", new { registrationId = registration.MaDangKy, matched = false, eventId = Guid.NewGuid().ToString("N") });
    var thirdFailure = await SendGateAsync("/api/gate/access", new { registrationId = registration.MaDangKy, matched = false, eventId = Guid.NewGuid().ToString("N") });
    thirdFailure.EnsureSuccessStatusCode();
    using var thirdFailureResult = JsonDocument.Parse(await thirdFailure.Content.ReadAsStringAsync());
    Ensure(thirdFailureResult.RootElement.GetProperty("requiresStaffAssistance").GetBoolean() &&
        thirdFailureResult.RootElement.GetProperty("failedAttempts").GetInt32() == 3,
        "The third distinct failed recognition must direct the member to staff without blocking the next person.");

    using var current = JsonDocument.Parse(await http.GetStringAsync("/api/member/subscriptions/current"));
    Ensure(current.RootElement.GetProperty("registrationId").GetInt32() == registration.MaDangKy, "A future pending renewal must not replace the currently usable paid subscription.");
    await db.DangKyGoiTap.Where(x => x.MaDangKy == registration.MaDangKy).ExecuteUpdateAsync(set => set.SetProperty(x => x.TrangThai, "DaHuy"));
    Ensure((int)(await http.GetAsync("/api/member/subscriptions/current")).StatusCode == 204, "Only a pending future renewal means there is no current subscription.");
    await db.Users.Where(x => x.Id == memberUser.Id).ExecuteUpdateAsync(set => set.SetProperty(x => x.TrangThai, "NgungHoatDong"));
    Ensure((int)(await http.GetAsync("/api/member/me")).StatusCode == 401, "Disabling an account must revoke an already issued member token.");
    await db.Users.Where(x => x.Id == memberUser.Id).ExecuteUpdateAsync(set => set.SetProperty(x => x.TrangThai, "HoatDong"));
    async Task LoginMemberAsync(string secret)
    {
        var elapsed = Stopwatch.StartNew();
        var response = await http.PostAsJsonAsync("/api/auth/login", new { userNameOrEmail = memberUserName, password = secret });
        response.EnsureSuccessStatusCode();
        Console.WriteLine($"API test: member re-login completed in {elapsed.ElapsedMilliseconds} ms.");
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        http.DefaultRequestHeaders.Authorization = new("Bearer", payload.RootElement.GetProperty("accessToken").GetString());
    }
    await LoginMemberAsync(memberPassword);
    var services = new ServiceCollection();
    services.AddLogging(); services.AddDataProtection(); services.AddFitGymInfrastructure(connectionString);
    await using (var provider = services.BuildServiceProvider())
    await using (var scope = provider.CreateAsyncScope())
    {
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var account = (await manager.FindByIdAsync(memberUser.Id.ToString()))!;
        var reset = await manager.ResetPasswordAsync(account, await manager.GeneratePasswordResetTokenAsync(account), memberPassword + "Changed");
        Ensure(reset.Succeeded, "Member password reset must execute through actual Identity providers.");
    }
    Ensure((int)(await http.GetAsync("/api/member/me")).StatusCode == 401, "Password reset must invalidate the previous API token.");
    await LoginMemberAsync(memberPassword + "Changed");
    await db.ThanhToan.Where(x => x.MaDangKy == pendingRenewal.MaDangKy).ExecuteDeleteAsync();
    await db.DangKyGoiTap.Where(x => x.MaDangKy == pendingRenewal.MaDangKy).ExecuteDeleteAsync();
    await db.DangKyGoiTap.Where(x => x.MaDangKy == registration.MaDangKy)
        .ExecuteUpdateAsync(set => set.SetProperty(x => x.TrangThai, "HieuLuc"));
    var packagesResponse = await http.GetAsync("/api/member/packages");
    packagesResponse.EnsureSuccessStatusCode();
    using var packages = JsonDocument.Parse(await packagesResponse.Content.ReadAsStringAsync());
    Ensure(packages.RootElement.EnumerateArray().Any(x => x.GetProperty("packageId").GetInt32() == package.MaGoiTap),
        "Member API must list actual active packages for renewal.");
    var quoteUrl = $"/api/member/renewals/quote?registrationId={registration.MaDangKy}&packageId={package.MaGoiTap}";
    var quoteResponse = await http.GetAsync(quoteUrl);
    quoteResponse.EnsureSuccessStatusCode();
    using var renewalQuote = JsonDocument.Parse(await quoteResponse.Content.ReadAsStringAsync());
    var quoteStart = renewalQuote.RootElement.GetProperty("startDate").GetDateTime();
    var quoteEnd = renewalQuote.RootElement.GetProperty("endDate").GetDateTime();
    var quoteTotal = renewalQuote.RootElement.GetProperty("totalAmount").GetDecimal();
    Ensure(quoteStart.Date == registration.NgayKetThuc.Date.AddDays(1) && quoteTotal == package.GiaTien,
        "Online renewal quote must follow the existing paid period and use the database price.");
    var renewalRequest = new
    {
        sourceRegistrationId = registration.MaDangKy,
        packageId = package.MaGoiTap,
        expectedStartDate = quoteStart,
        expectedEndDate = quoteEnd,
        expectedTotalAmount = quoteTotal,
        requestKey = Guid.NewGuid().ToString("N")
    };
    var simultaneousRenewals = await Task.WhenAll(
        http.PostAsJsonAsync("/api/member/renewals", renewalRequest),
        http.PostAsJsonAsync("/api/member/renewals", renewalRequest));
    foreach (var response in simultaneousRenewals) response.EnsureSuccessStatusCode();
    using var renewalResult = JsonDocument.Parse(await simultaneousRenewals[0].Content.ReadAsStringAsync());
    var onlineId = renewalResult.RootElement.GetProperty("registrationId").GetInt32();
    using var simultaneousResult = JsonDocument.Parse(await simultaneousRenewals[1].Content.ReadAsStringAsync());
    Ensure(simultaneousResult.RootElement.GetProperty("registrationId").GetInt32() == onlineId,
        "Concurrent submissions of one renewal must resolve to the same registration.");
    var repeatedRenewal = await http.PostAsJsonAsync("/api/member/renewals", renewalRequest);
    repeatedRenewal.EnsureSuccessStatusCode();
    using var repeatedResult = JsonDocument.Parse(await repeatedRenewal.Content.ReadAsStringAsync());
    Ensure(repeatedResult.RootElement.GetProperty("registrationId").GetInt32() == onlineId &&
        await db.DangKyGoiTap.CountAsync(x => x.MaHoiVien == member.MaHoiVien && x.TrangThai == "ChoThanhToan") == 1,
        "Retrying an online renewal must return the original request without creating another charge.");
    var onlineRegistration = await db.DangKyGoiTap.AsNoTracking().SingleAsync(x => x.MaDangKy == onlineId);
    var onlinePayment = await db.ThanhToan.AsNoTracking().SingleAsync(x => x.MaDangKy == onlineId);
    Ensure(onlineRegistration.MaNhanVien == null && onlinePayment.MaNhanVien == null &&
        onlineRegistration.TrangThai == "ChoThanhToan" && onlinePayment.TrangThai == "ChoThanhToan" &&
        onlinePayment.SoTien == quoteTotal,
        "Online renewal must not invent an employee or activate access before payment confirmation.");
    var qrResponse = await http.GetAsync($"/api/member/payments/{onlinePayment.MaThanhToan}/qr");
    qrResponse.EnsureSuccessStatusCode();
    var qrBytes = await qrResponse.Content.ReadAsByteArrayAsync();
    Ensure(qrResponse.Content.Headers.ContentType?.MediaType == "image/png" && qrBytes.Length > 8 &&
        qrBytes[0] == 137 && qrBytes[1] == 80 && qrBytes[2] == 78 && qrBytes[3] == 71 &&
        await db.ThanhToan.AsNoTracking().AnyAsync(x => x.MaThanhToan == onlinePayment.MaThanhToan && x.TrangThai == "ChoThanhToan"),
        "The member QR endpoint must return an image without marking the payment successful.");
    using var onlinePayments = JsonDocument.Parse(await http.GetStringAsync("/api/member/payments"));
    Ensure(onlinePayments.RootElement.EnumerateArray().Any(x => x.GetProperty("registrationId").GetInt32() == onlineId),
        "A member must be able to see the pending renewal payment.");
    var staffPayment = await new MembershipPaymentQuery(db).GetAsync(onlinePayment.MaThanhToan);
    Ensure(staffPayment is not null && staffPayment.EmployeeName == "Trực tuyến",
        "Staff payment lookup must include online renewals that have no creating employee.");
    var confirmation = await new MembershipPaymentConfirmation(db).ConfirmAsync(onlinePayment.MaThanhToan, employee.MaNhanVien);
    Ensure(confirmation.Succeeded &&
        await db.DangKyGoiTap.AsNoTracking().AnyAsync(x => x.MaDangKy == onlineId && x.TrangThai == "HieuLuc") &&
        await db.ThanhToan.AsNoTracking().AnyAsync(x => x.MaThanhToan == onlinePayment.MaThanhToan && x.TrangThai == "ThanhCong" && x.MaNhanVien == employee.MaNhanVien),
        "Confirming payment must atomically activate the online renewal and record the confirming employee.");
    var staffRole = await db.Roles.SingleAsync(x => x.Name == "NhanVien");
    db.UserRoles.Add(new IdentityUserRole<int> { UserId = memberUser.Id, RoleId = staffRole.Id }); await db.SaveChangesAsync();
    Ensure((int)(await http.GetAsync("/api/member/me")).StatusCode == 401, "Role changes must invalidate previously issued claims.");

    Console.WriteLine("member API functional scenarios: PASS");
}
finally
{
    if (apiProcess is { HasExited: false }) apiProcess.Kill(entireProcessTree: true);
    Console.WriteLine("API test: cleaning fixture...");
    await CleanupAsync(db);
}

static AppUser CreateUser(string userName, string email) => new()
{
    UserName = userName,
    NormalizedUserName = userName.ToUpperInvariant(),
    Email = email,
    NormalizedEmail = email.ToUpperInvariant(),
    EmailConfirmed = true,
    SecurityStamp = Guid.NewGuid().ToString(),
    ConcurrencyStamp = Guid.NewGuid().ToString(),
    TrangThai = "HoatDong"
};

static Process StartApi(string connectionString)
{
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    var apiProject = Path.Combine(root, "src", "FitGym.Api");
    var dll = Path.Combine(apiProject, "bin", "Debug", "net9.0", "FitGym.Api.dll");
    var startInfo = new ProcessStartInfo("dotnet", $"\"{dll}\" --urls {apiUrl}")
    {
        WorkingDirectory = apiProject,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };
    startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
    startInfo.Environment["ConnectionStrings__FitGym"] = connectionString;
    startInfo.Environment["Gate__ApiKey"] = gateKey;
    return Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start FitGym.Api.");
}

static async Task WaitUntilReadyAsync(HttpClient http, Process process)
{
    for (var attempt = 0; attempt < 40; attempt++)
    {
        if (process.HasExited)
        {
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"FitGym.Api exited during startup.\n{output}\n{error}");
        }
        try
        {
            var response = await http.GetAsync("/api/health");
            if (response.IsSuccessStatusCode) return;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            // API is still starting.
        }
        await Task.Delay(250);
    }
    throw new InvalidOperationException("FitGym.Api did not become ready within 10 seconds.");
}

static async Task CleanupAsync(FitGymDbContext db)
{
    // Identity reset runs in another scope and changes ConcurrencyStamp.
    // Reload fixture identities before deleting, rather than using stale tracked stamps.
    db.ChangeTracker.Clear();
    var memberIds = await db.HoiVien.Where(x => x.HoTen.StartsWith(testMarker)).Select(x => x.MaHoiVien).ToListAsync();
    var employeeIds = await db.NhanVien.Where(x => x.HoTen.StartsWith(testMarker)).Select(x => x.MaNhanVien).ToListAsync();
    var packageIds = await db.GoiTap.Where(x => x.TenGoi.StartsWith(testMarker)).Select(x => x.MaGoiTap).ToListAsync();
    var registrationIds = await db.DangKyGoiTap
        .Where(x => memberIds.Contains(x.MaHoiVien) || packageIds.Contains(x.MaGoiTap))
        .Select(x => x.MaDangKy)
        .ToListAsync();

    db.LichSuCheckIn.RemoveRange(db.LichSuCheckIn.Where(x => registrationIds.Contains(x.MaDangKy)));
    db.ThanhToan.RemoveRange(db.ThanhToan.Where(x => x.MaDangKy != null && registrationIds.Contains(x.MaDangKy.Value)));
    db.DuLieuKhuonMat.RemoveRange(db.DuLieuKhuonMat.Where(x => memberIds.Contains(x.MaHoiVien)));
    db.DongYSinhTracHoc.RemoveRange(db.DongYSinhTracHoc.Where(x => memberIds.Contains(x.MaHoiVien)));
    db.DangKyGoiTap.RemoveRange(db.DangKyGoiTap.Where(x => registrationIds.Contains(x.MaDangKy)));
    db.HoiVien.RemoveRange(db.HoiVien.Where(x => memberIds.Contains(x.MaHoiVien)));
    db.GoiTap.RemoveRange(db.GoiTap.Where(x => packageIds.Contains(x.MaGoiTap)));
    db.NhanVien.RemoveRange(db.NhanVien.Where(x => employeeIds.Contains(x.MaNhanVien)));
    await db.SaveChangesAsync();

    db.Users.RemoveRange(db.Users.Where(x => x.UserName == memberUserName || x.UserName == "fitgym_api_employee_test"));
    await db.SaveChangesAsync();
}

static void Ensure(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
