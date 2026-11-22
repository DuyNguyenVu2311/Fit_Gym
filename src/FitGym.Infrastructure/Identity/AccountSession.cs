using System.Security.Claims;
using FitGym.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace FitGym.Infrastructure.Identity;

public static class AccountSession
{
    public static async Task<bool> IsCurrentAsync(ClaimsPrincipal? principal, UserManager<AppUser> users)
    {
        if (principal?.Identity?.IsAuthenticated != true) return false;
        var user = await users.GetUserAsync(principal);
        if (user is null || user.TrangThai != "HoatDong") return false;
        var stamp = principal.FindFirstValue(users.Options.ClaimsIdentity.SecurityStampClaimType);
        if (string.IsNullOrEmpty(stamp) || stamp != await users.GetSecurityStampAsync(user)) return false;
        var claimedRoles = principal.FindAll(users.Options.ClaimsIdentity.RoleClaimType).Select(x => x.Value).Order().ToArray();
        var actualRoles = (await users.GetRolesAsync(user)).Order().ToArray();
        return claimedRoles.SequenceEqual(actualRoles);
    }
}
