using System.Diagnostics;
using System.Net;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using FitGym.Application.Models;
using FitGym.Application.Features.CheckIns;
using FitGym.Application.Features.Registrations;
using FitGym.Infrastructure.Features.CheckIns;
using FitGym.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var runId = Guid.NewGuid().ToString("N")[..12];
var marker = "__FITGYM_APPROVED_WORKFLOW__" + runId + "__";
var username = "fitgym_workflow_" + runId;
const string password = "Workflow@Test2026!";
var connection = Environment.GetEnvironmentVariable("FITGYM_TEST_CONNECTION")
    ?? "Server=.\\SQLEXPRESS;Database=FitGymDev;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
await using var db = new FitGymDbContext(new DbContextOptionsBuilder<FitGymDbContext>().UseSqlServer(connection).Options);
Process? process = null;
try
{
    var user = new AppUser { UserName = username, NormalizedUserName = username.ToUpperInvariant(), SecurityStamp = Guid.NewGuid().ToString(), ConcurrencyStamp = Guid.NewGuid().ToString(), TrangThai = "HoatDong" };
    user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, password);
    db.Users.Add(user); await db.SaveChangesAsync();
    var role = await db.Roles.SingleAsync(x => x.Name == "QuanLy");
    db.UserRoles.Add(new IdentityUserRole<int> { UserId = user.Id, RoleId = role.Id });
    var employee = new NhanVien { MaTaiKhoan = user.Id, HoTen = marker, ChucVu = "Test", NgayVaoLam = DateTime.Today, TrangThai = "HoatDong" };
    var member = new HoiVien { HoTen = marker, SoDienThoai = "0900000098", NgayThamGia = DateTime.Today, TrangThai = "HoatDong" };
    var package = new GoiTap { TenGoi = marker, ThoiHanThang = 1, GiaTien = 765432.25m, TrangThai = "HoatDong" };
    var inactive = new GoiTap { TenGoi = marker + "INACTIVE", ThoiHanThang = 1, GiaTien = 1m, TrangThai = "NgungHoatDong" };
    db.AddRange(employee, member, package, inactive); await db.SaveChangesAsync();

    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    var web = Path.Combine(root, "src", "FitGym.Web");
    var info = new ProcessStartInfo("dotnet", $"\"{Path.Combine(web, "bin", "Debug", "net9.0", "FitGym.Web.dll")}\" --urls http://127.0.0.1:5218")
    { WorkingDirectory = web, UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
    info.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
    info.Environment["ConnectionStrings__FitGym"] = connection;
    info.Environment["Logging__LogLevel__Default"] = "Warning";
    info.Environment["FITGYM_BOOTSTRAP_ADMIN_USER"] = "";
    info.Environment["FITGYM_BOOTSTRAP_ADMIN_PASSWORD"] = "";
    process = Process.Start(info)!;
    process.BeginOutputReadLine(); process.BeginErrorReadLine();
    using var handler = new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() };
    using var http = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:5218"), Timeout = TimeSpan.FromSeconds(3) };
    for (var attempt = 0; ; attempt++)
    {
        try { if ((await http.GetAsync("/Account/Login")).IsSuccessStatusCode) break; }
        catch (HttpRequestException) { }
        if (attempt >= 40 || process.HasExited) throw new Exception("Web failed to start.");
        await Task.Delay(200);
    }
    Ensure((await http.GetAsync("/Registrations/Quote?packageId=1")).StatusCode == HttpStatusCode.Redirect, "Quote requires authentication.");
    Ensure((await http.GetAsync("/Organization")).StatusCode == HttpStatusCode.Redirect, "Organization settings require authentication.");
    Ensure((await http.GetAsync("/Home/Snapshot")).StatusCode == HttpStatusCode.Redirect, "Dashboard refresh must require authentication.");
    Ensure((await http.GetAsync("/Payments")).StatusCode == HttpStatusCode.Redirect, "Payment lookup requires staff authentication.");
    Ensure((await http.GetAsync("/Reports")).StatusCode == HttpStatusCode.Redirect, "Manager report requires authentication.");
    var login = await http.GetStringAsync("/Account/Login");
    Ensure(login.Contains("name=\"Role\"") && login.Contains("ForgotPassword"), "Login must offer staff/manager selection and a password-help link.");
    Ensure(!login.Contains("QUẢN TRỊ VẬN HÀNH · FIT_GYM")
        && !login.Contains("Chọn vai trò và đăng nhập bằng tài khoản của bạn")
        && !login.Contains("Quyền truy cập được xác định")
        && !login.Contains("Sử dụng Fit_Gym"), "Login must not show redundant operational copy.");
    var passwordHelp = await http.GetStringAsync("/Account/ForgotPassword");
    Ensure(passwordHelp.Contains("quản lý") && passwordHelp.Contains("đơn vị triển khai"), "Password help must describe the existing recovery responsibilities.");
    var missingRole = await PostAsync("/Account/Login", login, new() { ["UserNameOrEmail"] = username, ["Password"] = password });
    Ensure(missingRole.StatusCode == HttpStatusCode.OK && (await http.GetAsync("/Accounts")).StatusCode == HttpStatusCode.Redirect, "Login must require the assigned role selection.");
    var wrongRole = await PostAsync("/Account/Login", login, new() { ["UserNameOrEmail"] = username, ["Password"] = password, ["Role"] = "NhanVien" });
    Ensure(wrongRole.StatusCode == HttpStatusCode.OK && (await http.GetAsync("/Accounts")).StatusCode == HttpStatusCode.Redirect, "Manager credentials must not sign in through the staff selection.");
    var auth = await PostAsync("/Account/Login", login, new() { ["UserNameOrEmail"] = username, ["Password"] = password, ["Role"] = "QuanLy" });
    Ensure(auth.StatusCode == HttpStatusCode.Redirect, "Test employee must log in.");
    var accountPage = await http.GetStringAsync("/Accounts");
    Ensure(WebUtility.HtmlDecode(accountPage).Contains("Tài khoản & nhân viên") && accountPage.Contains("Đặt lại mật khẩu"), "Manager account page must expose staff account administration.");
    var createAccountForm = await http.GetStringAsync("/Accounts/Create");
    Ensure(!createAccountForm.Contains("name=\"Email\""), "Staff account creation must not require an email address.");
    var staffNoEmailName = username + "_staff_no_email";
    var createdStaff = await PostAsync("/Accounts/Create", createAccountForm, new()
    {
        ["UserName"] = staffNoEmailName, ["Password"] = password, ["HoTen"] = marker,
        ["ChucVu"] = "Nhân viên", ["NgayVaoLam"] = DateTime.Today.ToString("yyyy-MM-dd"), ["Role"] = "NhanVien"
    });
    Ensure(createdStaff.StatusCode == HttpStatusCode.Redirect
        && await db.Users.AsNoTracking().AnyAsync(x => x.UserName == staffNoEmailName && x.Email == null),
        "Manager must be able to create a staff login without an email address.");
    var snapshot = await http.GetAsync("/Home/Snapshot?date=2026-11-18");
    var snapshotHtml = await snapshot.Content.ReadAsStringAsync();
    Ensure(snapshot.IsSuccessStatusCode && snapshotHtml.Contains("data-date=\"2026-11-18\"") && !snapshotHtml.Contains("<html"), "Snapshot must render filtered Razor content without duplicating the document layout.");
    Ensure(!snapshotHtml.Contains("Điều khiển cửa") && !snapshotHtml.Contains("Thiết bị nhận diện: Chưa kết nối"), "Operations dashboard must not advertise unused door control or device placeholder panels.");
    Ensure(snapshot.Headers.CacheControl?.NoStore == true, "Authenticated snapshots must not be cached.");
    Ensure((await http.GetAsync("/Home/Snapshot?date=9999-12-31")).StatusCode == HttpStatusCode.BadRequest, "Out-of-range dashboard date cannot crash aggregation.");
    var resetForm = await http.GetStringAsync($"/Accounts/ResetPassword/{user.Id}");
    var resetResult = await PostAsync("/Accounts/ResetPassword", resetForm, new() { ["UserId"] = user.Id.ToString(), ["UserName"] = username, ["NewPassword"] = password, ["ConfirmPassword"] = password });
    Ensure(resetResult.StatusCode == HttpStatusCode.Redirect, "Manager password reset must work with registered Identity token providers.");
    Ensure((await http.GetAsync("/Accounts")).StatusCode == HttpStatusCode.Redirect, "A password reset must revoke an existing browser session.");
    login = await http.GetStringAsync("/Account/Login");
    await PostAsync("/Account/Login", login, new() { ["UserNameOrEmail"] = username, ["Password"] = password, ["Role"] = "QuanLy" });

    var unlinked = new AppUser { UserName = username + "_unlinked", NormalizedUserName = (username + "_unlinked").ToUpperInvariant(), SecurityStamp = Guid.NewGuid().ToString(), ConcurrencyStamp = Guid.NewGuid().ToString(), TrangThai = "HoatDong" };
    unlinked.PasswordHash = new PasswordHasher<AppUser>().HashPassword(unlinked, password);
    db.Users.Add(unlinked); await db.SaveChangesAsync();
    db.UserRoles.Add(new IdentityUserRole<int> { UserId = unlinked.Id, RoleId = role.Id }); await db.SaveChangesAsync();
    var editUnlinked = await http.GetAsync($"/Accounts/Edit/{unlinked.Id}");
    Ensure(editUnlinked.IsSuccessStatusCode, "An unlinked manager must be able to complete actual employee details.");
    var unlinkedHtml = await editUnlinked.Content.ReadAsStringAsync();
    var employmentInput = Regex.Match(unlinkedHtml, "<input[^>]*name=\"NgayVaoLam\"[^>]*>").Value;
    Ensure(!employmentInput.Contains("value=\"" + DateTime.Today.ToString("yyyy-MM-dd")), "An unknown employment date must remain empty until staff enters the actual date.");
    var linkedEmployee = await PostAsync("/Accounts/Edit", await editUnlinked.Content.ReadAsStringAsync(), new() { ["UserId"] = unlinked.Id.ToString(), ["UserName"] = unlinked.UserName!, ["Email"] = "workflow-unlinked@example.test", ["HoTen"] = marker, ["ChucVu"] = "Test", ["NgayVaoLam"] = DateTime.Today.ToString("yyyy-MM-dd"), ["Role"] = "QuanLy", ["TrangThai"] = "HoatDong" });
    Ensure(linkedEmployee.StatusCode == HttpStatusCode.Redirect && await db.NhanVien.AnyAsync(x => x.MaTaiKhoan == unlinked.Id), "Submitted employee details must create the missing link without a fabricated automatic profile.");
    var organizationForm = await http.GetStringAsync("/Organization");
    Ensure(organizationForm.Contains("OrganizationName") && organizationForm.Contains("SiteAddress"), "Manager can render actual operator setup.");
    Ensure((await http.PostAsync("/Organization/Setup", new FormUrlEncodedContent(new Dictionary<string, string>()))).StatusCode == HttpStatusCode.BadRequest, "Operator writes require antiforgery protection.");

    var packageForm = await http.GetStringAsync("/Packages/Create");
    var packageValues = new Dictionary<string, string> { ["TenGoi"] = marker + "UI", ["ThoiHanThang"] = "1", ["GiaTien"] = "-1" };
    var invalidPackage = await PostAsync("/Packages/Create", packageForm, new(packageValues));
    Ensure(invalidPackage.StatusCode == HttpStatusCode.OK && !await db.GoiTap.AnyAsync(x => x.TenGoi == marker + "UI"), "Invalid negative package price must not persist.");
    packageValues["GiaTien"] = "450000";
    var createdPackage = await PostAsync("/Packages/Create", packageForm, new(packageValues));
    Ensure(createdPackage.StatusCode == HttpStatusCode.Redirect, "Manager can create an actual package through the Web form.");
    var uiPackageId = await db.GoiTap.Where(x => x.TenGoi == marker + "UI").Select(x => x.MaGoiTap).SingleAsync();
    packageValues["MaGoiTap"] = uiPackageId.ToString(); packageValues["GiaTien"] = "480000"; packageValues["TrangThai"] = "HoatDong";
    var editedPackage = await PostAsync("/Packages/Edit", await http.GetStringAsync($"/Packages/Edit/{uiPackageId}"), packageValues);
    Ensure(editedPackage.StatusCode == HttpStatusCode.Redirect && await db.GoiTap.AnyAsync(x => x.MaGoiTap == uiPackageId && x.GiaTien == 480000m), "Package update must persist actual submitted price.");

    var form = await http.GetStringAsync("/Registrations/Create");
    Ensure(form.Contains("data-registration-wizard"), "Registration must expose the approved five-step desk wizard.");
    var date = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
    var quote = await http.GetAsync($"/Registrations/Quote?memberId={member.MaHoiVien}&packageId={package.MaGoiTap}&startDate={date}");
    quote.EnsureSuccessStatusCode();
    using var json = JsonDocument.Parse(await quote.Content.ReadAsStringAsync());
    Ensure(json.RootElement.GetProperty("total").GetDecimal() == package.GiaTien, "Quote reads actual package price.");
    Ensure(json.RootElement.GetProperty("endDate").GetString() == DateTime.Today.AddDays(1).AddMonths(1).ToString("yyyy-MM-dd"), "Quote uses server calendar-month terms.");
    var tomorrowQuoteToken = json.RootElement.GetProperty("quoteToken").GetString();
    Ensure((await http.GetAsync($"/Registrations/Quote?memberId={member.MaHoiVien}&packageId={inactive.MaGoiTap}&startDate={date}")).StatusCode == HttpStatusCode.BadRequest, "Inactive package cannot be quoted.");
    Ensure((await http.GetAsync($"/Registrations/Quote?memberId={member.MaHoiVien}&packageId={package.MaGoiTap}&startDate=9999-12-31")).StatusCode == HttpStatusCode.BadRequest, "Overflow date must return a validation response.");

    var data = new Dictionary<string, string>
    {
        ["MaHoiVien"] = member.MaHoiVien.ToString(), ["MaGoiTap"] = package.MaGoiTap.ToString(),
        ["NgayBatDau"] = DateTime.Today.ToString("yyyy-MM-dd"), ["PhuongThucThanhToan"] = "TienMat", ["DongYSinhTracHoc"] = "true",
        ["TongTien"] = "1", ["NgayKetThuc"] = "2099-01-01"
    };
    using var currentQuote = JsonDocument.Parse(await http.GetStringAsync($"/Registrations/Quote?memberId={member.MaHoiVien}&packageId={package.MaGoiTap}&startDate={DateTime.Today:yyyy-MM-dd}"));
    data["QuoteToken"] = currentQuote.RootElement.GetProperty("quoteToken").GetString()!;
    var unconfirmed = await PostAsync("/Registrations/Create", form, data);
    Ensure(unconfirmed.StatusCode == HttpStatusCode.OK && !await db.DangKyGoiTap.AnyAsync(x => x.MaHoiVien == member.MaHoiVien), "Unconfirmed cash must not create or activate registration.");
    data["CashReceived"] = "true";
    await db.GoiTap.Where(x => x.MaGoiTap == package.MaGoiTap).ExecuteUpdateAsync(set => set.SetProperty(x => x.GiaTien, package.GiaTien + 100000));
    var repriced = await PostAsync("/Registrations/Create", form, data);
    Ensure(repriced.StatusCode == HttpStatusCode.OK && !await db.DangKyGoiTap.AnyAsync(x => x.MaHoiVien == member.MaHoiVien), "Repricing after quote must require fresh cash acknowledgment, not collect a larger amount silently.");
    await db.GoiTap.Where(x => x.MaGoiTap == package.MaGoiTap).ExecuteUpdateAsync(set => set.SetProperty(x => x.GiaTien, package.GiaTien));
    var validToken = data["QuoteToken"];
    data["QuoteToken"] = tomorrowQuoteToken!;
    var wrongDateQuote = await PostAsync("/Registrations/Create", form, data);
    Ensure(wrongDateQuote.StatusCode == HttpStatusCode.OK && !await db.DangKyGoiTap.AnyAsync(x => x.MaHoiVien == member.MaHoiVien), "Quote for another start date must not authorize this registration.");
    data["QuoteToken"] = validToken;
    data["QuoteToken"] = "forged-quote-token";
    var forged = await PostAsync("/Registrations/Create", form, data);
    Ensure(forged.StatusCode == HttpStatusCode.OK && !await db.DangKyGoiTap.AnyAsync(x => x.MaHoiVien == member.MaHoiVien), "Forged quote token must not authorize a transaction.");
    data["QuoteToken"] = validToken;
    var confirmed = await PostAsync("/Registrations/Create", form, data);
    Ensure(confirmed.StatusCode == HttpStatusCode.Redirect, "Confirmed desk cash creates registration.");
    var saved = await db.DangKyGoiTap.AsNoTracking().SingleAsync(x => x.MaHoiVien == member.MaHoiVien);
    Ensure(saved.TongTien == package.GiaTien && saved.NgayKetThuc == DateTime.Today.AddMonths(1), "Posted total/end-date tampering must have no effect.");
    var replay = await PostAsync("/Registrations/Create", form, new(data));
    Ensure(replay.StatusCode == HttpStatusCode.Redirect && replay.Headers.Location == confirmed.Headers.Location && await db.DangKyGoiTap.CountAsync(x => x.MaHoiVien == member.MaHoiVien) == 1,
        "Replaying the same confirmed desk submission must return the original registration without a second charge.");
    var equivalentQuote = new Dictionary<string, string>(data) { ["QuoteToken"] = validToken + "    " };
    var equivalentReplay = await PostAsync("/Registrations/Create", form, equivalentQuote);
    Ensure(await db.DangKyGoiTap.CountAsync(x => x.MaHoiVien == member.MaHoiVien) == 1 &&
        equivalentReplay.StatusCode == HttpStatusCode.Redirect && equivalentReplay.Headers.Location == confirmed.Headers.Location,
        "An alternate accepted textual encoding of the same signed quote must never create a second charge.");
    await db.GoiTap.Where(x => x.MaGoiTap == package.MaGoiTap).ExecuteUpdateAsync(set => set.SetProperty(x => x.GiaTien, package.GiaTien + 100000));
    var paidReplayAfterReprice = await PostAsync("/Registrations/Create", form, new(data));
    Ensure(paidReplayAfterReprice.StatusCode == HttpStatusCode.Redirect && paidReplayAfterReprice.Headers.Location == confirmed.Headers.Location && await db.DangKyGoiTap.CountAsync(x => x.MaHoiVien == member.MaHoiVien) == 1,
        "A committed submission retried after catalog repricing must return its original receipt, not ask for another payment.");
    await db.GoiTap.Where(x => x.MaGoiTap == package.MaGoiTap).ExecuteUpdateAsync(set => set.SetProperty(x => x.GiaTien, package.GiaTien));
    var simultaneous = await Task.WhenAll(PostAsync("/Registrations/Create", form, new(data)), PostAsync("/Registrations/Create", form, new(data)));
    Ensure(simultaneous.All(x => x.StatusCode == HttpStatusCode.Redirect && x.Headers.Location == confirmed.Headers.Location) && await db.ThanhToan.CountAsync(x => x.MaDangKy == saved.MaDangKy) == 1,
        "Concurrent copies of one signed submission must keep exactly one registration and payment.");
    var raceMember = new HoiVien { HoTen = marker + "RACE", SoDienThoai = "0900000096", NgayThamGia = DateTime.Today, TrangThai = "HoatDong" };
    db.HoiVien.Add(raceMember); await db.SaveChangesAsync();
    using var raceQuote = JsonDocument.Parse(await http.GetStringAsync($"/Registrations/Quote?memberId={raceMember.MaHoiVien}&packageId={package.MaGoiTap}&startDate={DateTime.Today:yyyy-MM-dd}"));
    var raceValues = new Dictionary<string, string>(data) { ["MaHoiVien"] = raceMember.MaHoiVien.ToString(), ["QuoteToken"] = raceQuote.RootElement.GetProperty("quoteToken").GetString()! };
    var firstRace = await Task.WhenAll(PostAsync("/Registrations/Create", form, new(raceValues)), PostAsync("/Registrations/Create", form, new(raceValues)));
    Ensure(firstRace.All(x => x.StatusCode == HttpStatusCode.Redirect) && firstRace[0].Headers.Location == firstRace[1].Headers.Location && await db.DangKyGoiTap.CountAsync(x => x.MaHoiVien == raceMember.MaHoiVien) == 1,
        "Two simultaneous first submissions must commit one registration, not two charges or an orphan registration.");

    using (var otherHandler = new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() })
    using (var otherHttp = new HttpClient(otherHandler) { BaseAddress = http.BaseAddress })
    {
        string Token(string html) => WebUtility.HtmlDecode(Regex.Match(Regex.Match(html, "<input[^>]*name=\"__RequestVerificationToken\"[^>]*>").Value, "value=\"([^\"]+)\"").Groups[1].Value);
        var otherLogin = await otherHttp.GetStringAsync("/Account/Login");
        await otherHttp.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(otherLogin), ["UserNameOrEmail"] = unlinked.UserName!, ["Password"] = password, ["Role"] = "QuanLy" }));
        var otherForm = await otherHttp.GetStringAsync("/Registrations/Create");
        var ownerMember = new HoiVien { HoTen = marker + "OWNER", SoDienThoai = "0900000095", NgayThamGia = DateTime.Today, TrangThai = "HoatDong" };
        db.HoiVien.Add(ownerMember); await db.SaveChangesAsync();
        using var ownerQuote = JsonDocument.Parse(await http.GetStringAsync($"/Registrations/Quote?memberId={ownerMember.MaHoiVien}&packageId={package.MaGoiTap}&startDate={DateTime.Today:yyyy-MM-dd}"));
        var ownerValues = new Dictionary<string, string>(data) { ["MaHoiVien"] = ownerMember.MaHoiVien.ToString(), ["QuoteToken"] = ownerQuote.RootElement.GetProperty("quoteToken").GetString()! };
        var stolenQuote = new Dictionary<string, string>(ownerValues) { ["__RequestVerificationToken"] = Token(otherForm) };
        var deniedQuote = await otherHttp.PostAsync("/Registrations/Create", new FormUrlEncodedContent(stolenQuote));
        Ensure(deniedQuote.StatusCode == HttpStatusCode.OK && !await db.DangKyGoiTap.AnyAsync(x => x.MaHoiVien == ownerMember.MaHoiVien) &&
            WebUtility.HtmlDecode(await deniedQuote.Content.ReadAsStringAsync()).Contains("Thông tin giá/thời hạn chưa được xác nhận"), "An unused signed quote from another staff account must fail owner validation before any write.");
        var ownerSuccess = await PostAsync("/Registrations/Create", form, ownerValues);
        Ensure(ownerSuccess.StatusCode == HttpStatusCode.Redirect && await db.DangKyGoiTap.CountAsync(x => x.MaHoiVien == ownerMember.MaHoiVien) == 1, "The rightful quote owner must still be able to submit after an unauthorized attempt.");
    }
    Ensure(!await db.DuLieuKhuonMat.AnyAsync(x => x.MaHoiVien == member.MaHoiVien), "Consent must not fabricate face vectors.");
    Ensure(await db.DongYSinhTracHoc.AnyAsync(x => x.MaHoiVien == member.MaHoiVien), "Consent must be recorded separately.");

    var renewal = await http.GetStringAsync($"/Registrations/Renew/{saved.MaDangKy}");
    using var renewalQuote = JsonDocument.Parse(await http.GetStringAsync($"/Registrations/RenewalQuote?registrationId={saved.MaDangKy}&packageId={package.MaGoiTap}"));
    var renewalToken = renewalQuote.RootElement.GetProperty("quoteToken").GetString()!;
    Ensure(renewalQuote.RootElement.GetProperty("startDate").GetString() == saved.NgayKetThuc.AddDays(1).ToString("yyyy-MM-dd"), "Renewal preview must show the exact next term.");
    var cashRenewal = await PostAsync("/Registrations/Renew", renewal, new() { ["MaDangKyGoc"] = saved.MaDangKy.ToString(), ["MaGoiTap"] = package.MaGoiTap.ToString(), ["PhuongThucThanhToan"] = "TienMat" });
    Ensure(cashRenewal.StatusCode == HttpStatusCode.OK && await db.DangKyGoiTap.CountAsync(x => x.MaHoiVien == member.MaHoiVien) == 1, "Cash renewal must also require explicit received-cash acknowledgment.");
    await db.GoiTap.Where(x => x.MaGoiTap == package.MaGoiTap).ExecuteUpdateAsync(set => set.SetProperty(x => x.GiaTien, package.GiaTien + 100000));
    var changedRenewal = await PostAsync("/Registrations/Renew", renewal, new() { ["MaDangKyGoc"] = saved.MaDangKy.ToString(), ["MaGoiTap"] = package.MaGoiTap.ToString(), ["PhuongThucThanhToan"] = "TienMat", ["CashReceived"] = "true", ["QuoteToken"] = renewalToken });
    Ensure(changedRenewal.StatusCode == HttpStatusCode.OK && await db.DangKyGoiTap.CountAsync(x => x.MaHoiVien == member.MaHoiVien) == 1, "Renewal repricing must not silently mark the new amount paid.");
    await db.GoiTap.Where(x => x.MaGoiTap == package.MaGoiTap).ExecuteUpdateAsync(set => set.SetProperty(x => x.GiaTien, package.GiaTien));
    var renewed = await PostAsync("/Registrations/Renew", renewal, new() { ["MaDangKyGoc"] = saved.MaDangKy.ToString(), ["MaGoiTap"] = package.MaGoiTap.ToString(), ["PhuongThucThanhToan"] = "QR", ["QuoteToken"] = renewalToken });
    Ensure(renewed.StatusCode == HttpStatusCode.Redirect, "QR renewal creates a new pending record.");
    var renewedReplay = await PostAsync("/Registrations/Renew", renewal, new() { ["MaDangKyGoc"] = saved.MaDangKy.ToString(), ["MaGoiTap"] = package.MaGoiTap.ToString(), ["PhuongThucThanhToan"] = "QR", ["QuoteToken"] = renewalToken });
    Ensure(renewedReplay.StatusCode == HttpStatusCode.Redirect && renewedReplay.Headers.Location == renewed.Headers.Location &&
        await db.DangKyGoiTap.CountAsync(x => x.MaHoiVien == member.MaHoiVien) == 2,
        "Repeating a staff renewal submission must not create a second pending charge.");
    var pending = await db.DangKyGoiTap.AsNoTracking().SingleAsync(x => x.MaHoiVien == member.MaHoiVien && x.MaDangKy != saved.MaDangKy);
    Ensure(pending.TrangThai == RegistrationStatuses.PendingPayment, "QR must remain pending without provider confirmation.");
    var cashPaymentId = await db.ThanhToan.Where(x => x.MaDangKy == saved.MaDangKy).Select(x => x.MaThanhToan).SingleAsync();
    var pendingPaymentId = await db.ThanhToan.Where(x => x.MaDangKy == pending.MaDangKy).Select(x => x.MaThanhToan).SingleAsync();
    var paymentsList = await http.GetAsync("/Payments?search=" + Uri.EscapeDataString(member.HoTen));
    var paymentsHtml = await paymentsList.Content.ReadAsStringAsync();
    Ensure(paymentsList.IsSuccessStatusCode, "Week 5 must provide actual membership payment lookup. HTTP " + (int)paymentsList.StatusCode + ": " + paymentsHtml[..Math.Min(2500, paymentsHtml.Length)]);
    Ensure(paymentsHtml.Contains($"data-payment-id=\"{cashPaymentId}\"") && paymentsHtml.Contains($"data-payment-id=\"{pendingPaymentId}\""), "Payment lookup must include actual cash and pending QR records.");
    var pendingOnly = await http.GetStringAsync("/Payments?status=ChoThanhToan&search=" + Uri.EscapeDataString(member.HoTen));
    Ensure(pendingOnly.Contains($"data-payment-id=\"{pendingPaymentId}\"") && !pendingOnly.Contains($"data-payment-id=\"{cashPaymentId}\""), "Payment filtering must not relabel successful cash as pending.");
    var paymentDetail = await http.GetStringAsync($"/Payments/Details/{cashPaymentId}");
    Ensure(paymentDetail.Contains(saved.MaDangKy.ToString("D6")) && paymentDetail.Contains("data-amount=\"765432.25\""), "Payment detail must show persisted registration and exact paid amount.");
    var pendingDetail = await http.GetStringAsync($"/Payments/Details/{pendingPaymentId}");
    Ensure(pendingDetail.Contains($"/Payments/QrCode/{pendingPaymentId}") && !pendingDetail.Contains("mô phỏng", StringComparison.OrdinalIgnoreCase), "Pending payment must show a scannable QR without prototype labels.");
    var demoQr = await http.GetAsync($"/Payments/QrCode/{pendingPaymentId}");
    Ensure(demoQr.IsSuccessStatusCode && demoQr.Content.Headers.ContentType?.MediaType == "image/svg+xml" && (await demoQr.Content.ReadAsStringAsync()).Contains("<svg"), "QR must be generated as an SVG image.");
    Ensure((await db.ThanhToan.AsNoTracking().SingleAsync(x => x.MaThanhToan == pendingPaymentId)).TrangThai == PaymentStatuses.Pending, "Viewing or scanning demo QR must never mark payment successful.");
    var reportDate = DateTime.Today;
    var reportStartUtc = reportDate.AddHours(-7);
    var reportEndUtc = reportDate.AddDays(1).AddHours(-7);
    var expectedRevenue = await db.ThanhToan.AsNoTracking().Where(x => x.TrangThai == PaymentStatuses.Succeeded && x.MaDangKy != null && x.NgayThanhToan >= reportStartUtc && x.NgayThanhToan < reportEndUtc).SumAsync(x => (decimal?)x.SoTien) ?? 0m;
    var reportHtml = await http.GetStringAsync($"/Reports?from={reportDate:yyyy-MM-dd}&to={reportDate:yyyy-MM-dd}");
    Ensure(reportHtml.Contains($"data-revenue=\"{expectedRevenue.ToString("F2", CultureInfo.InvariantCulture)}\"") && reportHtml.Contains("Yêu cầu QR chờ") && !reportHtml.Contains("mô phỏng", StringComparison.OrdinalIgnoreCase), "Manager report must use persisted successful payments, show pending QR separately, and avoid prototype labels.");
    var visiblePaymentDetail = WebUtility.HtmlDecode(Regex.Replace(paymentDetail, "<[^>]+>", " "));
    var visiblePaymentList = WebUtility.HtmlDecode(Regex.Replace(paymentsHtml, "<[^>]+>", " "));
    Ensure(visiblePaymentDetail.Contains("765.432,25") && visiblePaymentList.Contains("765.432,25"), "Visible payment amounts must preserve persisted fractional digits using Vietnamese number formatting.");
    Ensure((await http.GetAsync("/Payments?from=2026-11-21&to=2026-11-20")).StatusCode == HttpStatusCode.BadRequest, "Reversed payment date range must be rejected.");
    Ensure((await http.GetAsync("/Payments?status=InventedStatus")).StatusCode == HttpStatusCode.BadRequest, "Unknown payment filter status must not silently change meaning.");
    foreach (var invalidFilter in new[] { "method=InventedMethod", "page=0", "page=2147483647", "from=invalid", "to=9999-12-31" })
        Ensure((await http.GetAsync("/Payments?" + invalidFilter)).StatusCode == HttpStatusCode.BadRequest, "Invalid payment filter must return 400: " + invalidFilter);
    var cashRecordedAt = await db.ThanhToan.Where(x => x.MaThanhToan == cashPaymentId).Select(x => x.NgayThanhToan).SingleAsync();
    var pendingRecordedAt = await db.ThanhToan.Where(x => x.MaThanhToan == pendingPaymentId).Select(x => x.NgayThanhToan).SingleAsync();
    await db.ThanhToan.Where(x => x.MaThanhToan == cashPaymentId).ExecuteUpdateAsync(set => set.SetProperty(x => x.NgayThanhToan, new DateTime(2026, 11, 19, 18, 0, 0)));
    await db.ThanhToan.Where(x => x.MaThanhToan == pendingPaymentId).ExecuteUpdateAsync(set => set.SetProperty(x => x.NgayThanhToan, new DateTime(2026, 11, 19, 16, 59, 0)));
    var vietnamDay = await http.GetStringAsync("/Payments?from=2026-11-20&to=2026-11-20&search=" + Uri.EscapeDataString(member.HoTen));
    Ensure(vietnamDay.Contains($"data-payment-id=\"{cashPaymentId}\"") && !vietnamDay.Contains($"data-payment-id=\"{pendingPaymentId}\""), "Vietnam civil-day filter must include 01:00 local and exclude 23:59 the previous day, even when both UTC dates are the same.");
    var secondPage = await http.GetStringAsync("/Payments?page=2&search=" + Uri.EscapeDataString(member.HoTen));
    Ensure(!secondPage.Contains("data-payment-id="), "Payment page 2 must not repeat page 1 when fewer than 50 records match.");
    await db.ThanhToan.Where(x => x.MaThanhToan == cashPaymentId).ExecuteUpdateAsync(set => set.SetProperty(x => x.NgayThanhToan, cashRecordedAt));
    await db.ThanhToan.Where(x => x.MaThanhToan == pendingPaymentId).ExecuteUpdateAsync(set => set.SetProperty(x => x.NgayThanhToan, pendingRecordedAt));
    Ensure((await http.GetAsync("/Payments/Details/2147483647")).StatusCode == HttpStatusCode.NotFound, "Unknown payment must return 404.");
    Ensure(pending.NgayBatDau == saved.NgayKetThuc.AddDays(1), "Renewal must follow previous term without overwriting it.");
    var checkIns = new CheckInService(db);
    Ensure(!(await checkIns.RecordManualAsync(pending.MaDangKy, "Test")).Succeeded, "Pending QR cannot pass manual check-in.");
    var unverifiedConfirmation = await PostAsync($"/Payments/Confirm/{pendingPaymentId}", pendingDetail, new());
    Ensure(unverifiedConfirmation.StatusCode == HttpStatusCode.OK
        && (await db.ThanhToan.AsNoTracking().SingleAsync(x => x.MaThanhToan == pendingPaymentId)).TrangThai == PaymentStatuses.Pending,
        "A QR payment cannot be confirmed without explicit receipt verification.");
    var confirmedPayment = await PostAsync($"/Payments/Confirm/{pendingPaymentId}", pendingDetail, new() { ["ReceiptVerified"] = "true" });
    Ensure(confirmedPayment.StatusCode == HttpStatusCode.Redirect
        && (await db.ThanhToan.AsNoTracking().SingleAsync(x => x.MaThanhToan == pendingPaymentId)).TrangThai == PaymentStatuses.Succeeded
        && (await db.DangKyGoiTap.AsNoTracking().SingleAsync(x => x.MaDangKy == pending.MaDangKy)).TrangThai == RegistrationStatuses.Active,
        "Confirmed payment must activate the related registration in the same operation.");
    var repeatedConfirmation = await PostAsync($"/Payments/Confirm/{pendingPaymentId}", pendingDetail, new() { ["ReceiptVerified"] = "true" });
    Ensure(repeatedConfirmation.StatusCode == HttpStatusCode.OK, "A QR payment cannot be confirmed twice.");
    Ensure((await checkIns.RecordManualAsync(saved.MaDangKy, "Test")).Succeeded, "Paid current registration can check in.");
    db.DuLieuKhuonMat.Add(new DuLieuKhuonMat { MaHoiVien = member.MaHoiVien, VectorKhuonMat = [1, 2, 3], PhienBanMoHinh = "workflow-test-only", NgayTao = DateTime.UtcNow, DangHoatDong = true });
    await db.SaveChangesAsync();
    var firstFace = await checkIns.RecordFaceAttemptAsync(saved.MaDangKy, false);
    var secondFace = await checkIns.RecordFaceAttemptAsync(saved.MaDangKy, false);
    var thirdFace = await checkIns.RecordFaceAttemptAsync(saved.MaDangKy, false);
    var fourthFace = await checkIns.RecordFaceAttemptAsync(saved.MaDangKy, true);
    Ensure(firstFace.FailedAttempts == 1 && secondFace.FailedAttempts == 2 && thirdFace.FailedAttempts == 3 && thirdFace.RequiresStaffAssistance, "Third failed face attempt must ask for staff assistance.");
    Ensure(!fourthFace.Succeeded && fourthFace.RequiresStaffAssistance && await db.LichSuCheckIn.CountAsync(x => x.MaDangKy == saved.MaDangKy && x.TrangThai == CheckInStatuses.FaceFailed) == 3, "A fourth face attempt for the same member must not be accepted or logged in the active attempt window.");
    Ensure((await checkIns.RecordManualAsync(saved.MaDangKy, "Test")).Succeeded, "Staff can still assist a member after three failed face attempts.");
    await db.DangKyGoiTap.Where(x => x.MaDangKy == saved.MaDangKy).ExecuteUpdateAsync(set => set.SetProperty(x => x.NgayBatDau, DateTime.Today.AddMonths(-2)).SetProperty(x => x.NgayKetThuc, DateTime.Today.AddDays(-1)));
    Ensure(!(await checkIns.RecordManualAsync(saved.MaDangKy, "Test")).Succeeded, "Expired registration cannot be bypassed by staff.");
    await db.DangKyGoiTap.Where(x => x.MaDangKy == saved.MaDangKy).ExecuteUpdateAsync(set => set.SetProperty(x => x.NgayBatDau, saved.NgayBatDau).SetProperty(x => x.NgayKetThuc, saved.NgayKetThuc));
    await db.HoiVien.Where(x => x.MaHoiVien == member.MaHoiVien).ExecuteUpdateAsync(set => set.SetProperty(x => x.TrangThai, "NgungHoatDong"));
    Ensure(!(await checkIns.RecordManualAsync(saved.MaDangKy, "Test")).Succeeded, "Inactive member cannot pass manual check-in even with paid membership.");
    await db.HoiVien.Where(x => x.MaHoiVien == member.MaHoiVien).ExecuteUpdateAsync(set => set.SetProperty(x => x.TrangThai, "HoatDong"));

    var memberForm = await http.GetStringAsync("/Members/Create?returnToRegistration=true");
    var newMember = await PostAsync("/Members/Create", memberForm, new() { ["HoTen"] = marker + "NEW", ["SoDienThoai"] = "0900000097", ["NgayThamGia"] = DateTime.Today.ToString("yyyy-MM-dd"), ["ReturnToRegistration"] = "true" });
    Ensure(newMember.StatusCode == HttpStatusCode.Redirect && newMember.Headers.Location?.ToString().StartsWith("/Registrations/Create?memberId=") == true, "Creating member from wizard must return to registration with saved member ID.");
    foreach (var route in new[] { "/", "/Members", "/Packages", "/CheckIns", "/Goods", "/Equipment", "/Training/Profiles", "/Training/Exercises", "/Accounts" })
        Ensure((await http.GetAsync(route)).IsSuccessStatusCode, route + " must render after workflow changes.");

    var accountForm = await http.GetStringAsync($"/MemberAccounts/Create?memberId={member.MaHoiVien}");
    var accountValues = new Dictionary<string, string> { ["MemberId"] = member.MaHoiVien.ToString(), ["UserName"] = username + "_member", ["Password"] = password, ["ConfirmPassword"] = password };
    var createdAccount = await PostAsync("/MemberAccounts/Create", accountForm, accountValues);
    Ensure(createdAccount.StatusCode == HttpStatusCode.Redirect, "Desk staff can create an app account linked to an actual member.");
    var linkedUserId = await db.HoiVien.AsNoTracking().Where(x => x.MaHoiVien == member.MaHoiVien).Select(x => x.MaTaiKhoan).SingleAsync();
    Ensure(linkedUserId.HasValue && !await db.UserRoles.AnyAsync(x => x.UserId == linkedUserId), "Member account must not receive staff or manager permissions.");
    var memberPasswordHash = await db.Users.AsNoTracking().Where(x => x.Id == linkedUserId!.Value).Select(x => x.PasswordHash).SingleAsync();
    var employeeResetForm = await http.GetStringAsync($"/Accounts/ResetPassword/{user.Id}");
    var wrongAccountReset = await PostAsync("/Accounts/ResetPassword", employeeResetForm, new() { ["UserId"] = linkedUserId.Value.ToString(), ["UserName"] = username + "_member", ["NewPassword"] = password + "Wrong", ["ConfirmPassword"] = password + "Wrong" });
    Ensure(wrongAccountReset.StatusCode == HttpStatusCode.OK && (await db.Users.AsNoTracking().Where(x => x.Id == linkedUserId.Value).Select(x => x.PasswordHash).SingleAsync()) == memberPasswordHash, "Employee account administration must never reset a member app account.");
    Ensure((await http.GetStringAsync($"/Members/Details/{member.MaHoiVien}")).Contains(username + "_member"), "Staff can look up the linked member username without exposing its password.");
    var duplicateAccount = await PostAsync("/MemberAccounts/Create", accountForm, accountValues);
    Ensure(duplicateAccount.StatusCode == HttpStatusCode.OK && await db.Users.CountAsync(x => x.UserName == username + "_member") == 1, "Repeated provisioning must not create an orphan or duplicate account.");
    Ensure(!(await http.GetStringAsync("/Accounts")).Contains(username + "_member"), "Staff account list must not mix member app accounts.");
    var resetMemberForm = await http.GetStringAsync($"/MemberAccounts/ResetPassword?memberId={member.MaHoiVien}");
    var resetMember = await PostAsync("/MemberAccounts/ResetPassword", resetMemberForm, new() { ["MemberId"] = member.MaHoiVien.ToString(), ["Password"] = password, ["ConfirmPassword"] = password });
    Ensure(resetMember.StatusCode == HttpStatusCode.Redirect, "Manager can reset the linked app account password.");

    var anotherMemberId = await db.HoiVien.Where(x => x.HoTen == marker + "NEW").Select(x => x.MaHoiVien).SingleAsync();
    var concurrentForm = await http.GetStringAsync($"/MemberAccounts/Create?memberId={anotherMemberId}");
    var concurrent = await Task.WhenAll(
        PostAsync("/MemberAccounts/Create", concurrentForm, new() { ["MemberId"] = anotherMemberId.ToString(), ["UserName"] = username + "_race_a", ["Password"] = password, ["ConfirmPassword"] = password }),
        PostAsync("/MemberAccounts/Create", concurrentForm, new() { ["MemberId"] = anotherMemberId.ToString(), ["UserName"] = username + "_race_b", ["Password"] = password, ["ConfirmPassword"] = password }));
    Ensure(concurrent.Count(x => x.StatusCode == HttpStatusCode.Redirect) == 1 && concurrent.Count(x => x.StatusCode == HttpStatusCode.OK) == 1, "Concurrent provisioning must save one account and reject the other safely.");
    Ensure(await db.Users.CountAsync(x => x.UserName == username + "_race_a" || x.UserName == username + "_race_b") == 1, "Rejected concurrent provisioning cannot leave an orphan user.");

    using (var memberHandler = new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() })
    using (var memberHttp = new HttpClient(memberHandler) { BaseAddress = http.BaseAddress })
    {
        var memberLogin = await memberHttp.GetStringAsync("/Account/Login");
        var token = WebUtility.HtmlDecode(Regex.Match(Regex.Match(memberLogin, "<input[^>]*name=\"__RequestVerificationToken\"[^>]*>").Value, "value=\"([^\"]+)\"").Groups[1].Value);
        await memberHttp.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["UserNameOrEmail"] = username + "_member", ["Password"] = password, ["Role"] = "NhanVien" }));
        Ensure((await memberHttp.GetAsync("/")).StatusCode == HttpStatusCode.Redirect, "A valid member password must never grant access to the staff dashboard.");
    }

    // Re-authenticate the fixture as ordinary staff; never change a real account.
    db.UserRoles.RemoveRange(db.UserRoles.Where(x => x.UserId == user.Id));
    await db.SaveChangesAsync();
    var staffRole = await db.Roles.SingleAsync(x => x.Name == "NhanVien");
    db.UserRoles.Add(new IdentityUserRole<int> { UserId = user.Id, RoleId = staffRole.Id });
    await db.SaveChangesAsync();
    foreach (Cookie cookie in handler.CookieContainer.GetAllCookies()) cookie.Expired = true;
    login = await http.GetStringAsync("/Account/Login");
    var staffWrongRole = await PostAsync("/Account/Login", login, new() { ["UserNameOrEmail"] = username, ["Password"] = password, ["Role"] = "QuanLy" });
    Ensure(staffWrongRole.StatusCode == HttpStatusCode.OK, "Employee credentials must not sign in through the manager selection.");
    await PostAsync("/Account/Login", login, new() { ["UserNameOrEmail"] = username, ["Password"] = password, ["Role"] = "NhanVien" });
    var staffHome = WebUtility.HtmlDecode(await http.GetStringAsync("/"));
    Ensure(!staffHome.Contains("Tài khoản & nhân viên") && !staffHome.Contains("Báo cáo vận hành"), "Staff navigation must not display manager-only destinations.");
    var staffSettings = await http.GetAsync("/Organization");
    Ensure(staffSettings.StatusCode == HttpStatusCode.Redirect && staffSettings.Headers.Location!.ToString().Contains("AccessDenied"), "Ordinary staff cannot access operator settings.");
    var staffAccounts = await http.GetAsync("/Accounts");
    Ensure(staffAccounts.StatusCode == HttpStatusCode.Redirect && staffAccounts.Headers.Location!.ToString().Contains("AccessDenied"), "Ordinary staff cannot administer employee accounts.");
    var staffReport = await http.GetAsync("/Reports");
    Ensure(staffReport.StatusCode == HttpStatusCode.Redirect && staffReport.Headers.Location!.ToString().Contains("AccessDenied"), "Ordinary staff cannot view manager reports.");
    var staffWrite = await PostAsync("/Organization/Setup", await http.GetStringAsync("/Registrations/Create"), new() { ["OrganizationName"] = "forbidden" });
    Ensure(staffWrite.StatusCode == HttpStatusCode.Redirect && staffWrite.Headers.Location!.ToString().Contains("AccessDenied"), "Ordinary staff cannot configure operator data.");
    var staffReset = await http.GetAsync($"/MemberAccounts/ResetPassword?memberId={member.MaHoiVien}");
    Ensure(staffReset.StatusCode == HttpStatusCode.Redirect && staffReset.Headers.Location!.ToString().Contains("AccessDenied"), "Ordinary staff cannot reset member credentials.");
    Ensure((await http.GetAsync("/Payments")).IsSuccessStatusCode && (await http.GetAsync($"/Payments/Details/{cashPaymentId}")).IsSuccessStatusCode, "Desk staff can read actual membership payments.");
    var staffPackageCreate = await http.GetAsync("/Packages/Create");
    Ensure(staffPackageCreate.StatusCode == HttpStatusCode.Redirect && staffPackageCreate.Headers.Location!.ToString().Contains("AccessDenied"), "Desk staff cannot create package catalog entries.");
    Console.WriteLine("approved Web workflow behavioral scenarios: PASS");

    async Task<HttpResponseMessage> PostAsync(string route, string html, Dictionary<string, string> values)
    {
        var input = Regex.Match(html, "<input[^>]*name=\"__RequestVerificationToken\"[^>]*>").Value;
        values["__RequestVerificationToken"] = WebUtility.HtmlDecode(Regex.Match(input, "value=\"([^\"]+)\"").Groups[1].Value);
        return await http.PostAsync(route, new FormUrlEncodedContent(values));
    }
}
finally
{
    if (process is { HasExited: false }) { process.Kill(true); await process.WaitForExitAsync(); }
    await CleanupAsync();
}

async Task CleanupAsync()
{
    db.ChangeTracker.Clear();
    var ids = await db.HoiVien.Where(x => x.HoTen.StartsWith(marker)).Select(x => x.MaHoiVien).ToListAsync();
    var registrations = await db.DangKyGoiTap.Where(x => ids.Contains(x.MaHoiVien)).Select(x => x.MaDangKy).ToListAsync();
    db.LichSuCheckIn.RemoveRange(db.LichSuCheckIn.Where(x => registrations.Contains(x.MaDangKy)));
    db.ThanhToan.RemoveRange(db.ThanhToan.Where(x => x.MaDangKy.HasValue && registrations.Contains(x.MaDangKy.Value)));
    db.DangKyGoiTap.RemoveRange(db.DangKyGoiTap.Where(x => registrations.Contains(x.MaDangKy)));
    db.DongYSinhTracHoc.RemoveRange(db.DongYSinhTracHoc.Where(x => ids.Contains(x.MaHoiVien)));
    db.DuLieuKhuonMat.RemoveRange(db.DuLieuKhuonMat.Where(x => ids.Contains(x.MaHoiVien)));
    db.HoiVien.RemoveRange(db.HoiVien.Where(x => ids.Contains(x.MaHoiVien)));
    db.GoiTap.RemoveRange(db.GoiTap.Where(x => x.TenGoi.StartsWith(marker)));
    db.NhanVien.RemoveRange(db.NhanVien.Where(x => x.HoTen == marker));
    await db.SaveChangesAsync();
    var users = db.Users.Where(x => x.UserName == username || x.UserName == username + "_member" || x.UserName == username + "_unlinked" || x.UserName == username + "_race_a" || x.UserName == username + "_race_b" || x.UserName == username + "_staff_no_email");
    db.UserRoles.RemoveRange(db.UserRoles.Where(x => users.Select(u => u.Id).Contains(x.UserId)));
    db.Users.RemoveRange(users); await db.SaveChangesAsync();
}
static void Ensure(bool condition, string message) { if (!condition) throw new Exception(message); }
