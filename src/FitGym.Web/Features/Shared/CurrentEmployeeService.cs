using FitGym.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitGym.Web.Features.Shared;

public sealed class CurrentEmployeeService(
    UserManager<AppUser> userManager,
    FitGymDbContext dbContext)
{
    public async Task<int?> GetEmployeeIdAsync(
        System.Security.Claims.ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var userName = principal.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userName)) return null;
        var user = await userManager.FindByNameAsync(userName);
        if (user is null) return null;
        return await dbContext.NhanVien.AsNoTracking()
            .Where(x => x.MaTaiKhoan == user.Id && x.TrangThai == "HoatDong")
            .Select(x => (int?)x.MaNhanVien)
            .SingleOrDefaultAsync(cancellationToken);
    }
}

