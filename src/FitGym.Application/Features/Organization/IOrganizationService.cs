using FitGym.Application.Common;

namespace FitGym.Application.Features.Organization;

public sealed record OrganizationSetupRequest(string OrganizationName, string? Phone, string? Email, string SiteName, string SiteAddress);
public sealed record OrganizationUpdateRequest(string OrganizationName, string? Phone, string? Email, string SiteName, string SiteAddress, byte[]? Logo, bool RemoveLogo);
public sealed record OrganizationProfile(string OrganizationName, string? Phone, string? Email, string SiteName, string SiteAddress, bool HasLogo, int SiteCount);
public sealed record PublicOrganizationProfile(bool IsConfigured, string DisplayName, string? SiteName, bool HasLogo);
public sealed record OrganizationLogo(byte[] Content, string ContentType);

public interface IOrganizationService
{
    Task<OrganizationProfile?> GetAsync(CancellationToken cancellationToken = default);
    Task<PublicOrganizationProfile> GetPublicAsync(CancellationToken cancellationToken = default);
    Task<OrganizationLogo?> GetLogoAsync(CancellationToken cancellationToken = default);
    Task<OperationResult> SetupAsync(OrganizationSetupRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateAsync(OrganizationUpdateRequest request, CancellationToken cancellationToken = default);
}
