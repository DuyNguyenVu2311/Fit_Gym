using FitGym.Application.Security;
using FitGym.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FitGym.Infrastructure.Identity;

public static class FitGymIdentityInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        bool isDevelopment,
        ILogger logger)
    {
        await using var scope = services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

        foreach (var roleName in FitGymRoles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var roleResult = await roleManager.CreateAsync(new IdentityRole<int>(roleName));
            if (!roleResult.Succeeded)
                throw new InvalidOperationException(FormatErrors($"Không thể tạo vai trò {roleName}", roleResult.Errors));
        }

        if (!isDevelopment)
            return;

        var bootstrapUserName = Environment.GetEnvironmentVariable("FITGYM_BOOTSTRAP_ADMIN_USER")?.Trim();
        var bootstrapPassword = Environment.GetEnvironmentVariable("FITGYM_BOOTSTRAP_ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(bootstrapUserName) || string.IsNullOrWhiteSpace(bootstrapPassword))
        {
            logger.LogInformation(
                "Không tạo tài khoản quản lý ban đầu. Đặt FITGYM_BOOTSTRAP_ADMIN_USER và FITGYM_BOOTSTRAP_ADMIN_PASSWORD khi cần khởi tạo môi trường phát triển.");
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var existingUser = await userManager.FindByNameAsync(bootstrapUserName);
        if (existingUser is not null)
            return;

        var user = new AppUser
        {
            UserName = bootstrapUserName,
            Email = bootstrapUserName.Contains('@') ? bootstrapUserName : null,
            EmailConfirmed = bootstrapUserName.Contains('@'),
            TrangThai = "HoatDong"
        };

        var createResult = await userManager.CreateAsync(user, bootstrapPassword);
        if (!createResult.Succeeded)
            throw new InvalidOperationException(FormatErrors("Không thể tạo tài khoản quản lý ban đầu", createResult.Errors));

        var roleResultForUser = await userManager.AddToRoleAsync(user, FitGymRoles.QuanLy);
        if (!roleResultForUser.Succeeded)
        {
            await userManager.DeleteAsync(user);
            throw new InvalidOperationException(FormatErrors("Không thể gán vai trò quản lý", roleResultForUser.Errors));
        }

        logger.LogInformation("Đã tạo tài khoản quản lý ban đầu cho môi trường phát triển: {UserName}", bootstrapUserName);
    }

    private static string FormatErrors(string prefix, IEnumerable<IdentityError> errors) =>
        $"{prefix}: {string.Join("; ", errors.Select(error => error.Description))}";
}
