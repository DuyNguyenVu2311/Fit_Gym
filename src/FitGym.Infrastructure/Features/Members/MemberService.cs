using FitGym.Application.Common;
using FitGym.Application.Features.Members;
using FitGym.Application.Models;
using FitGym.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitGym.Infrastructure.Features.Members;

public sealed class MemberService(FitGymDbContext dbContext) : IMemberService
{
    public async Task<IReadOnlyList<MemberSummary>> SearchAsync(
        string? search,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.HoiVien.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.HoTen.Contains(term) || x.SoDienThoai.Contains(term));
        }

        if (MemberStatuses.All.Contains(status, StringComparer.Ordinal))
            query = query.Where(x => x.TrangThai == status);

        return await query
            .OrderBy(x => x.HoTen)
            .Select(x => new MemberSummary(x.MaHoiVien, x.HoTen, x.SoDienThoai, x.NgayThamGia, x.TrangThai))
            .ToListAsync(cancellationToken);
    }

    public async Task<MemberDetail?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        await dbContext.HoiVien.AsNoTracking()
            .Where(x => x.MaHoiVien == id)
            .Select(x => new MemberDetail(
                x.MaHoiVien, x.MaTaiKhoan, x.HoTen, x.NgaySinh, x.GioiTinh,
                x.DiaChi, x.SoDienThoai, x.NgayThamGia, x.TrangThai))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<OperationResult<int>> CreateAsync(
        MemberCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = Validate(request.HoTen, request.NgaySinh, request.SoDienThoai, request.NgayThamGia);
        if (validationError is not null) return OperationResult<int>.Failure(validationError);

        var phone = request.SoDienThoai.Trim();
        if (await dbContext.HoiVien.AnyAsync(x => x.SoDienThoai == phone, cancellationToken))
            return OperationResult<int>.Failure("Số điện thoại đã được sử dụng cho hội viên khác.");

        var member = new HoiVien
        {
            HoTen = request.HoTen.Trim(),
            NgaySinh = request.NgaySinh?.Date,
            GioiTinh = NormalizeOptional(request.GioiTinh),
            DiaChi = NormalizeOptional(request.DiaChi),
            SoDienThoai = phone,
            NgayThamGia = request.NgayThamGia.Date,
            TrangThai = MemberStatuses.Active
        };
        dbContext.HoiVien.Add(member);
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Success(member.MaHoiVien);
    }

    public async Task<OperationResult> UpdateAsync(
        MemberUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = Validate(request.HoTen, request.NgaySinh, request.SoDienThoai, request.NgayThamGia);
        if (validationError is not null) return OperationResult.Failure(validationError);
        if (!MemberStatuses.All.Contains(request.TrangThai, StringComparer.Ordinal))
            return OperationResult.Failure("Trạng thái hội viên không hợp lệ.");

        var member = await dbContext.HoiVien.SingleOrDefaultAsync(x => x.MaHoiVien == request.MaHoiVien, cancellationToken);
        if (member is null) return OperationResult.Failure("Không tìm thấy hội viên.");

        var phone = request.SoDienThoai.Trim();
        if (await dbContext.HoiVien.AnyAsync(
                x => x.SoDienThoai == phone && x.MaHoiVien != request.MaHoiVien,
                cancellationToken))
            return OperationResult.Failure("Số điện thoại đã được sử dụng cho hội viên khác.");

        member.HoTen = request.HoTen.Trim();
        member.NgaySinh = request.NgaySinh?.Date;
        member.GioiTinh = NormalizeOptional(request.GioiTinh);
        member.DiaChi = NormalizeOptional(request.DiaChi);
        member.SoDienThoai = phone;
        member.NgayThamGia = request.NgayThamGia.Date;
        member.TrangThai = request.TrangThai;
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SetStatusAsync(
        int id,
        string status,
        CancellationToken cancellationToken = default)
    {
        if (!MemberStatuses.All.Contains(status, StringComparer.Ordinal))
            return OperationResult.Failure("Trạng thái hội viên không hợp lệ.");
        var member = await dbContext.HoiVien.SingleOrDefaultAsync(x => x.MaHoiVien == id, cancellationToken);
        if (member is null) return OperationResult.Failure("Không tìm thấy hội viên.");

        member.TrangThai = status == MemberStatuses.Active ? MemberStatuses.Active : "NgungHoatDong";
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    private static string? Validate(string name, DateTime? birthDate, string phone, DateTime joinedDate)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Vui lòng nhập họ tên hội viên.";
        if (string.IsNullOrWhiteSpace(phone)) return "Vui lòng nhập số điện thoại.";
        if (birthDate?.Date > DateTime.Today) return "Ngày sinh không thể ở tương lai.";
        if (joinedDate == default) return "Ngày tham gia không hợp lệ.";
        return null;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

