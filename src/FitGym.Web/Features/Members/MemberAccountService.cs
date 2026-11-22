using System.Data;
using System.Text.RegularExpressions;
using FitGym.Application.Common;
using FitGym.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FitGym.Web.Features.Members;

public sealed class MemberAccountService(FitGymDbContext db, UserManager<AppUser> users)
{
    public Task<string?> GetUserNameAsync(int memberId, CancellationToken cancellationToken) =>
        (from member in db.HoiVien.AsNoTracking()
         join user in db.Users.AsNoTracking() on member.MaTaiKhoan equals user.Id
         where member.MaHoiVien == memberId
         select user.UserName).SingleOrDefaultAsync(cancellationToken);

    public async Task<OperationResult> CreateAsync(int memberId, string userName, string password, CancellationToken cancellationToken)
    {
        userName = userName.Trim();
        if (!Regex.IsMatch(userName, "^[a-zA-Z0-9._-]{3,100}$"))
            return OperationResult.Failure("Tên đăng nhập cần 3–100 ký tự chữ không dấu, số, dấu chấm, gạch dưới hoặc gạch ngang.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            // Serialize provisioning for this member; concurrent submissions cannot create orphan accounts.
            var member = await db.HoiVien.FromSqlInterpolated($"SELECT * FROM HOI_VIEN WITH (UPDLOCK, HOLDLOCK) WHERE MaHoiVien = {memberId}").SingleOrDefaultAsync(cancellationToken);
            if (member is null || member.TrangThai != "HoatDong") return OperationResult.Failure("Hội viên không tồn tại hoặc đã ngừng hoạt động.");
            if (member.MaTaiKhoan.HasValue) return OperationResult.Failure("Hội viên đã có tài khoản ứng dụng.");
            if (await users.FindByNameAsync(userName) is not null) return OperationResult.Failure("Tên đăng nhập đã được sử dụng.");
            var user = new AppUser { UserName = userName, TrangThai = "HoatDong" };
            var created = await users.CreateAsync(user, password);
            if (!created.Succeeded) return OperationResult.Failure(string.Join(" ", created.Errors.Select(x => x.Description)));
            member.MaTaiKhoan = user.Id;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OperationResult.Success();
        }
        catch (Exception error) when (error is SqlException { Number: 1205 or 2601 or 2627 } || error is DbUpdateException { InnerException: SqlException { Number: 1205 or 2601 or 2627 } })
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            return OperationResult.Failure("Thông tin tài khoản vừa thay đổi. Vui lòng tải lại hồ sơ và thử lại.");
        }
    }

    public async Task<OperationResult> ResetPasswordAsync(int memberId, string password, CancellationToken cancellationToken)
    {
        var accountId = await db.HoiVien.AsNoTracking().Where(x => x.MaHoiVien == memberId).Select(x => x.MaTaiKhoan).SingleOrDefaultAsync(cancellationToken);
        var user = accountId.HasValue ? await users.FindByIdAsync(accountId.Value.ToString()) : null;
        if (user is null || (await users.GetRolesAsync(user)).Count != 0 || await db.NhanVien.AnyAsync(x => x.MaTaiKhoan == user.Id, cancellationToken))
            return OperationResult.Failure("Không tìm thấy tài khoản hội viên phù hợp.");
        var result = await users.ResetPasswordAsync(user, await users.GeneratePasswordResetTokenAsync(user), password);
        return result.Succeeded ? OperationResult.Success() : OperationResult.Failure(string.Join(" ", result.Errors.Select(x => x.Description)));
    }
}
