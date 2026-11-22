using System.Data;
using System.Net.Mail;
using FitGym.Application.Common;
using FitGym.Application.Features.Organization;
using FitGym.Application.Models;
using FitGym.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FitGym.Infrastructure.Features.Organization;

public sealed class OrganizationService(FitGymDbContext db) : IOrganizationService
{
    public async Task<OrganizationProfile?> GetAsync(CancellationToken cancellationToken = default)
    {
        var organization = await db.DonViSuDung.AsNoTracking().Where(x => x.MaDonVi == 1)
            .Select(x => new { x.TenDonVi, x.SoDienThoai, x.Email, HasLogo = x.Logo != null }).SingleOrDefaultAsync(cancellationToken);
        if (organization is null) return null;
        var sites = await db.CoSo.AsNoTracking().Where(x => x.MaDonVi == 1).OrderBy(x => x.MaCoSo).ToListAsync(cancellationToken);
        var first = sites.FirstOrDefault();
        return new(organization.TenDonVi, organization.SoDienThoai, organization.Email, first?.TenCoSo ?? "", first?.DiaChi ?? "", organization.HasLogo, sites.Count);
    }

    public async Task<PublicOrganizationProfile> GetPublicAsync(CancellationToken cancellationToken = default)
    {
        var profile = await GetAsync(cancellationToken);
        return profile is null ? new(false, "Fit_Gym", null, false) : new(true, profile.OrganizationName, profile.SiteName, profile.HasLogo);
    }

    public async Task<OrganizationLogo?> GetLogoAsync(CancellationToken cancellationToken = default)
    {
        var row = await db.DonViSuDung.AsNoTracking().Where(x => x.MaDonVi == 1)
            .Select(x => new { x.Logo, x.LogoContentType }).SingleOrDefaultAsync(cancellationToken);
        return row?.Logo is null || row.LogoContentType is null ? null : new(row.Logo, row.LogoContentType);
    }

    public async Task<OperationResult> SetupAsync(OrganizationSetupRequest request, CancellationToken cancellationToken = default)
    {
        var error = Validate(request.OrganizationName, request.Phone, request.Email, request.SiteName, request.SiteAddress);
        if (error is not null) return OperationResult.Failure(error);
        await using var transaction = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken) : null;
        if (await db.DonViSuDung.AnyAsync(cancellationToken))
            return OperationResult.Failure("Đơn vị đã được thiết lập. Vui lòng dùng màn hình cập nhật thông tin.");
        if (await db.CoSo.AnyAsync(cancellationToken))
            return OperationResult.Failure("Đã có thông tin cơ sở cần kiểm tra trước khi thiết lập đơn vị.");
        try
        {
            db.DonViSuDung.Add(new DonViSuDung { MaDonVi = 1, TenDonVi = request.OrganizationName.Trim(), SoDienThoai = Optional(request.Phone), Email = Optional(request.Email) });
            db.CoSo.Add(new CoSo { MaDonVi = 1, TenCoSo = request.SiteName.Trim(), DiaChi = request.SiteAddress.Trim() });
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return OperationResult.Success();
        }
        catch (Exception exception) when (IsSetupConflict(exception))
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            return OperationResult.Failure("Thông tin thiết lập vừa được thay đổi bởi một phiên khác. Vui lòng tải lại trang.");
        }
    }

    public async Task<OperationResult> UpdateAsync(OrganizationUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var error = Validate(request.OrganizationName, request.Phone, request.Email, request.SiteName, request.SiteAddress);
        if (error is not null) return OperationResult.Failure(error);
        var mime = request.Logo is null ? null : LogoMime(request.Logo);
        if (request.Logo is not null && mime is null)
            return OperationResult.Failure("Logo phải là ảnh PNG/JPEG hợp lệ, tối đa 512 KiB.");
        if (request.Logo is not null && request.RemoveLogo)
            return OperationResult.Failure("Vui lòng chọn tải logo mới hoặc xóa logo hiện tại, không chọn đồng thời.");
        var organization = await db.DonViSuDung.SingleOrDefaultAsync(x => x.MaDonVi == 1, cancellationToken);
        if (organization is null) return OperationResult.Failure("Vui lòng thiết lập đơn vị trước khi cập nhật.");
        var site = await db.CoSo.Where(x => x.MaDonVi == 1).OrderBy(x => x.MaCoSo).FirstOrDefaultAsync(cancellationToken);
        if (site is null) return OperationResult.Failure("Không tìm thấy cơ sở đầu tiên. Vui lòng kiểm tra dữ liệu.");
        organization.TenDonVi = request.OrganizationName.Trim();
        organization.SoDienThoai = Optional(request.Phone);
        organization.Email = Optional(request.Email);
        site.TenCoSo = request.SiteName.Trim(); site.DiaChi = request.SiteAddress.Trim();
        if (request.RemoveLogo) { organization.Logo = null; organization.LogoContentType = null; }
        else if (request.Logo is not null) { organization.Logo = request.Logo; organization.LogoContentType = mime; }
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    private static string? Validate(string name, string? phone, string? email, string site, string address)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) return "Tên đơn vị là bắt buộc, tối đa 200 ký tự.";
        if (string.IsNullOrWhiteSpace(site) || site.Trim().Length > 200) return "Tên cơ sở đầu tiên là bắt buộc, tối đa 200 ký tự.";
        if (string.IsNullOrWhiteSpace(address) || address.Trim().Length > 500) return "Địa chỉ cơ sở là bắt buộc, tối đa 500 ký tự.";
        if (phone?.Trim().Length > 30) return "Số điện thoại không được vượt quá 30 ký tự.";
        if (!string.IsNullOrWhiteSpace(email) && (email.Trim().Length > 256 || !MailAddress.TryCreate(email.Trim(), out var parsed) || parsed.Address != email.Trim())) return "Email liên hệ không hợp lệ.";
        return null;
    }

    private static string? LogoMime(byte[] content)
    {
        if (content.Length is < 24 or > 524288) return null;
        if (content.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) && content.AsSpan(content.Length - 8, 4).SequenceEqual("IEND"u8)) return "image/png";
        if (content[0] == 255 && content[1] == 216 && content[^2] == 255 && content[^1] == 217) return "image/jpeg";
        return null;
    }
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool IsSetupConflict(Exception error) => error is SqlException { Number: 1205 or 2601 or 2627 } || error is DbUpdateException { InnerException: SqlException { Number: 1205 or 2601 or 2627 } };
}
