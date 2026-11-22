using FitGym.Application.Models;
using FitGym.Application.Security;
using FitGym.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitGym.Web.Features.Accounts;

public sealed record AccountOperationResult(bool Succeeded, string? Error = null)
{
    public static AccountOperationResult Success() => new(true);
    public static AccountOperationResult Failure(string error) => new(false, error);
}

public sealed class AccountAdministrationService(
    FitGymDbContext dbContext,
    UserManager<AppUser> userManager)
{
    public const string ActiveStatus = "HoatDong";
    public const string InactiveStatus = "NgungHoatDong";
    public const string CannotDisableCurrentUser = "Không thể vô hiệu hóa chính tài khoản đang đăng nhập.";

    public async Task<IReadOnlyList<AccountListItemViewModel>> ListAsync(string? search)
    {
        var query =
            from user in userManager.Users.AsNoTracking()
            join employee in dbContext.NhanVien.AsNoTracking()
                on user.Id equals employee.MaTaiKhoan into employeeGroup
            from employee in employeeGroup.DefaultIfEmpty()
            orderby user.UserName
            select new { User = user, Employee = employee };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                (x.User.UserName != null && x.User.UserName.Contains(term)) ||
                (x.Employee != null && x.Employee.HoTen.Contains(term)));
        }

        var rows = await query.ToListAsync();
        var result = new List<AccountListItemViewModel>(rows.Count);
        foreach (var row in rows)
        {
            var roles = await userManager.GetRolesAsync(row.User);
            if (!roles.Any(FitGymRoles.All.Contains)) continue;
            result.Add(new AccountListItemViewModel
            {
                UserId = row.User.Id,
                UserName = row.User.UserName ?? string.Empty,
                HoTen = row.Employee?.HoTen ?? string.Empty,
                ChucVu = row.Employee?.ChucVu ?? string.Empty,
                Role = roles.SingleOrDefault() ?? string.Empty,
                TrangThai = row.User.TrangThai
            });
        }

        return result;
    }

    public async Task<EditAccountViewModel?> GetEditAsync(int userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return null;

        var employee = await dbContext.NhanVien.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaTaiKhoan == userId);
        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Any(FitGymRoles.All.Contains) || await dbContext.HoiVien.AnyAsync(x => x.MaTaiKhoan == userId)) return null;
        return new EditAccountViewModel
        {
            UserId = user.Id,
            UserName = user.UserName ?? string.Empty,
            HoTen = employee?.HoTen ?? string.Empty,
            ChucVu = employee?.ChucVu ?? string.Empty,
            NgayVaoLam = employee?.NgayVaoLam,
            Role = roles.SingleOrDefault() ?? string.Empty,
            TrangThai = user.TrangThai
        };
    }

    public async Task<AccountOperationResult> CreateAsync(CreateAccountViewModel model)
    {
        var role = NormalizeRole(model.Role);
        if (role is null) return AccountOperationResult.Failure("Vai trò không hợp lệ.");
        if (await userManager.FindByNameAsync(model.UserName.Trim()) is not null)
            return AccountOperationResult.Failure("Tên đăng nhập đã tồn tại.");
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var user = new AppUser
        {
            UserName = model.UserName.Trim(),
            TrangThai = ActiveStatus
        };

        var createResult = await userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
            return AccountOperationResult.Failure(JoinErrors(createResult));

        dbContext.NhanVien.Add(new NhanVien
        {
            MaTaiKhoan = user.Id,
            HoTen = model.HoTen.Trim(),
            ChucVu = model.ChucVu.Trim(),
            NgayVaoLam = model.NgayVaoLam.Date,
            TrangThai = ActiveStatus
        });
        await dbContext.SaveChangesAsync();

        var roleResult = await userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
            return AccountOperationResult.Failure(JoinErrors(roleResult));

        await transaction.CommitAsync();
        return AccountOperationResult.Success();
    }

    public async Task<AccountOperationResult> UpdateAsync(EditAccountViewModel model, string currentUserName)
    {
        if (!model.NgayVaoLam.HasValue) return AccountOperationResult.Failure("Vui lòng nhập ngày vào làm thực tế.");
        var role = NormalizeRole(model.Role);
        if (role is null) return AccountOperationResult.Failure("Vai trò không hợp lệ.");
        if (model.TrangThai is not (ActiveStatus or InactiveStatus))
            return AccountOperationResult.Failure("Trạng thái không hợp lệ.");

        var user = await userManager.FindByIdAsync(model.UserId.ToString());
        if (user is null) return AccountOperationResult.Failure("Không tìm thấy tài khoản.");
        if (!(await userManager.GetRolesAsync(user)).Any(FitGymRoles.All.Contains) || await dbContext.HoiVien.AnyAsync(x => x.MaTaiKhoan == user.Id))
            return AccountOperationResult.Failure("Tài khoản này không phải tài khoản nhân viên.");
        var employee = await dbContext.NhanVien.SingleOrDefaultAsync(x => x.MaTaiKhoan == model.UserId);

        var isCurrentUser = string.Equals(user.UserName, currentUserName, StringComparison.OrdinalIgnoreCase);
        if (isCurrentUser && model.TrangThai == InactiveStatus)
            return AccountOperationResult.Failure(CannotDisableCurrentUser);
        if (isCurrentUser && role != FitGymRoles.QuanLy)
            return AccountOperationResult.Failure("Không thể tự gỡ vai trò Quản lý của tài khoản đang đăng nhập.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        user.TrangThai = model.TrangThai;
        if (employee is null)
        {
            employee = new NhanVien { MaTaiKhoan = user.Id };
            dbContext.NhanVien.Add(employee);
        }
        employee.HoTen = model.HoTen.Trim();
        employee.ChucVu = model.ChucVu.Trim();
        employee.NgayVaoLam = model.NgayVaoLam.Value.Date;
        employee.TrangThai = model.TrangThai;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return AccountOperationResult.Failure(JoinErrors(updateResult));

        var existingRoles = await userManager.GetRolesAsync(user);
        if (!existingRoles.Contains(role, StringComparer.Ordinal))
        {
            if (existingRoles.Count > 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, existingRoles);
                if (!removeResult.Succeeded)
                    return AccountOperationResult.Failure(JoinErrors(removeResult));
            }

            var addResult = await userManager.AddToRoleAsync(user, role);
            if (!addResult.Succeeded)
                return AccountOperationResult.Failure(JoinErrors(addResult));
        }

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return AccountOperationResult.Success();
    }

    public async Task<AccountOperationResult> ResetPasswordAsync(int userId, string newPassword)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return AccountOperationResult.Failure("Không tìm thấy tài khoản.");
        if (!(await userManager.GetRolesAsync(user)).Any(FitGymRoles.All.Contains) || await dbContext.HoiVien.AnyAsync(x => x.MaTaiKhoan == userId))
            return AccountOperationResult.Failure("Tài khoản này không phải tài khoản nhân viên.");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded
            ? AccountOperationResult.Success()
            : AccountOperationResult.Failure(JoinErrors(result));
    }

    private static string? NormalizeRole(string role) =>
        FitGymRoles.All.Contains(role, StringComparer.Ordinal) ? role : null;

    private static string JoinErrors(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(x => x.Description));
}
