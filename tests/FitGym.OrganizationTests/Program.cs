using FitGym.Application.Features.Organization;
using FitGym.Infrastructure.Features.Organization;
using FitGym.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using FitGym.Infrastructure;

Ensure(CustomerInstallation.Resolve(null, true).Id == "local-development", "Only Development may use a fallback installation identity.");
Ensure(CustomerInstallation.Resolve("gym-a", false).CookieName != CustomerInstallation.Resolve("gym-b", false).CookieName, "Independent installations must use separate auth cookie names.");
Ensure(CustomerInstallation.Resolve("gym-a", false).ApplicationName != CustomerInstallation.Resolve("gym-b", false).ApplicationName, "Data Protection purposes must be customer-specific.");
try { CustomerInstallation.Resolve(null, false); throw new Exception("Production without installation identity must fail."); }
catch (InvalidOperationException) { }

var connection = Environment.GetEnvironmentVariable("FITGYM_TEST_CONNECTION") ?? "Server=.\\SQLEXPRESS;Database=FitGymDev;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
await using var db = new FitGymDbContext(new DbContextOptionsBuilder<FitGymDbContext>().UseSqlServer(connection).Options);
// Roll back the entire scenario. Never remove or replace the user's real profile.
await using var transaction = await db.Database.BeginTransactionAsync();
try
{
    var service = new OrganizationService(db);
    if (await service.GetAsync() is not null) throw new Exception("Use an unconfigured test database for setup scenarios; existing organization was not changed.");
    var empty = await service.GetPublicAsync();
    Ensure(!empty.IsConfigured && empty.DisplayName == "Fit_Gym", "Unconfigured install must use product fallback, not a made-up gym.");
    var request = new OrganizationSetupRequest("__FITGYM_OPERATOR_TEST__", "0900000088", "operator@example.test", "__FITGYM_SITE_TEST__", "Địa chỉ cơ sở kiểm thử");
    Ensure(!(await service.SetupAsync(request with { OrganizationName = "" })).Succeeded, "Missing organization name must be rejected.");
    Ensure(!(await service.SetupAsync(request with { SiteName = "" })).Succeeded, "Initial real site name must be provided.");
    Ensure(!(await service.SetupAsync(request with { Email = "invalid-email" })).Succeeded, "Invalid contact email must be rejected before writing.");
    Ensure((await service.SetupAsync(request)).Succeeded, "Valid setup must persist operator and first site.");
    Ensure(await db.DonViSuDung.CountAsync() == 1 && await db.CoSo.CountAsync() == 1, "Setup must create exactly one operator and one site.");
    Ensure(!(await service.SetupAsync(request with { OrganizationName = "OTHER" })).Succeeded, "Setup cannot overwrite an existing operator.");
    Ensure((await service.GetAsync())!.OrganizationName == request.OrganizationName, "Rejected second setup must preserve the operator.");
    var update = new OrganizationUpdateRequest("Tên đơn vị đã cập nhật", request.Phone, request.Email, request.SiteName, request.SiteAddress, null, false);
    Ensure(!(await service.UpdateAsync(update with { Logo = "<svg onload='x()'/>"u8.ToArray() })).Succeeded, "SVG/active content must be rejected.");
    Ensure(!(await service.UpdateAsync(update with { Logo = new byte[600000] })).Succeeded, "Oversized logo must be rejected.");
    var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=");
    Ensure((await service.UpdateAsync(update with { Logo = png })).Succeeded, "Valid small PNG logo must be accepted.");
    Ensure(!(await service.UpdateAsync(update with { Logo = png, RemoveLogo = true })).Succeeded, "Upload and removal cannot occur in the same update.");
    var publicProfile = await service.GetPublicAsync();
    Ensure(publicProfile.DisplayName == update.OrganizationName && publicProfile.HasLogo, "Public branding reflects configured profile.");
    var json = JsonSerializer.Serialize(publicProfile);
    Ensure(!json.Contains(request.Phone!) && !json.Contains(request.Email!), "Public profile must not expose contact or account data.");
    Ensure((await service.GetLogoAsync())?.ContentType == "image/png", "Logo MIME is derived from signature, not user-supplied filename.");
    Ensure((await service.UpdateAsync(update with { RemoveLogo = true })).Succeeded && await service.GetLogoAsync() is null, "Explicit logo removal must work.");
    Console.WriteLine("organization setup/branding SQL scenarios: PASS (rolled back)");
}
finally { await transaction.RollbackAsync(); }
static void Ensure(bool value, string message) { if (!value) throw new Exception(message); }
