using FitGym.Application.Common;
using FitGym.Application.Features.Equipment;
using FitGym.Application.Features.Members;
using FitGym.Application.Models;
using FitGym.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitGym.Infrastructure.Features.Equipment;

public sealed class EquipmentService(FitGymDbContext dbContext) : IEquipmentService
{
    public async Task<IReadOnlyList<EquipmentSummary>> SearchAsync(
        string? search,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ThietBi.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.TenThietBi.Contains(term) || x.PhanLoai.Contains(term));
        }
        if (EquipmentStatuses.All.Contains(status, StringComparer.Ordinal))
            query = query.Where(x => x.TrangThai == status);

        return await query.OrderBy(x => x.TenThietBi)
            .Select(x => new EquipmentSummary(
                x.MaThietBi,
                x.TenThietBi,
                x.PhanLoai,
                x.NgayMua,
                x.TrangThai,
                dbContext.LichSuBaoTri.Count(m => m.MaThietBi == x.MaThietBi),
                dbContext.LichSuBaoTri
                    .Where(m => m.MaThietBi == x.MaThietBi)
                    .Max(m => (DateTime?)m.NgayBaoTri)))
            .ToListAsync(cancellationToken);
    }

    public async Task<EquipmentDetail?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var equipment = await dbContext.ThietBi.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaThietBi == id, cancellationToken);
        if (equipment is null) return null;
        var hasHistory = await dbContext.LichSuBaoTri.AsNoTracking().AnyAsync(x => x.MaThietBi == id, cancellationToken);
        var history = await dbContext.LichSuBaoTri.AsNoTracking()
            .Where(x => x.MaThietBi == id)
            .OrderByDescending(x => x.NgayBaoTri)
            .ThenByDescending(x => x.MaBaoTri)
            .Select(x => new MaintenanceItem(x.MaBaoTri, x.MaNhanVien, x.NgayBaoTri, x.MoTa, x.ChiPhi))
            .ToListAsync(cancellationToken);
        return new EquipmentDetail(
            equipment.MaThietBi, equipment.TenThietBi, equipment.PhanLoai,
            equipment.NgayMua, equipment.TrangThai, hasHistory, history);
    }

    public async Task<OperationResult<int>> CreateAsync(
        EquipmentCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var error = ValidateEquipment(request.TenThietBi, request.PhanLoai, request.NgayMua);
        if (error is not null) return OperationResult<int>.Failure(error);
        var equipment = new ThietBi
        {
            TenThietBi = request.TenThietBi.Trim(),
            PhanLoai = request.PhanLoai.Trim(),
            NgayMua = request.NgayMua.Date,
            TrangThai = EquipmentStatuses.Active
        };
        dbContext.ThietBi.Add(equipment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Success(equipment.MaThietBi);
    }

    public async Task<OperationResult> UpdateAsync(
        EquipmentUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var error = ValidateEquipment(request.TenThietBi, request.PhanLoai, request.NgayMua);
        if (error is not null) return OperationResult.Failure(error);
        if (!EquipmentStatuses.All.Contains(request.TrangThai, StringComparer.Ordinal))
            return OperationResult.Failure("Trạng thái thiết bị không hợp lệ.");
        var equipment = await dbContext.ThietBi.SingleOrDefaultAsync(x => x.MaThietBi == request.MaThietBi, cancellationToken);
        if (equipment is null) return OperationResult.Failure("Không tìm thấy thiết bị.");
        equipment.TenThietBi = request.TenThietBi.Trim();
        equipment.PhanLoai = request.PhanLoai.Trim();
        equipment.NgayMua = request.NgayMua.Date;
        equipment.TrangThai = request.TrangThai;
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult<int>> AddMaintenanceAsync(
        MaintenanceCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.NgayBaoTri.Date > DateTime.Today)
            return OperationResult<int>.Failure("Ngày bảo trì không thể ở tương lai.");
        if (string.IsNullOrWhiteSpace(request.MoTa))
            return OperationResult<int>.Failure("Vui lòng nhập nội dung bảo trì.");
        if (request.ChiPhi < 0)
            return OperationResult<int>.Failure("Chi phí bảo trì không thể âm.");
        if (!await dbContext.ThietBi.AsNoTracking().AnyAsync(x => x.MaThietBi == request.MaThietBi, cancellationToken))
            return OperationResult<int>.Failure("Không tìm thấy thiết bị.");
        if (!await dbContext.NhanVien.AsNoTracking().AnyAsync(
                x => x.MaNhanVien == request.MaNhanVien && x.TrangThai == MemberStatuses.Active,
                cancellationToken))
            return OperationResult<int>.Failure("Nhân viên ghi nhận bảo trì không hợp lệ.");
        var maintenance = new LichSuBaoTri
        {
            MaThietBi = request.MaThietBi,
            MaNhanVien = request.MaNhanVien,
            NgayBaoTri = request.NgayBaoTri.Date,
            MoTa = request.MoTa.Trim(),
            ChiPhi = request.ChiPhi
        };
        dbContext.LichSuBaoTri.Add(maintenance);
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Success(maintenance.MaBaoTri);
    }

    private static string? ValidateEquipment(string name, string category, DateTime purchaseDate)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Vui lòng nhập tên thiết bị.";
        if (string.IsNullOrWhiteSpace(category)) return "Vui lòng nhập phân loại.";
        if (purchaseDate == default || purchaseDate.Date > DateTime.Today)
            return "Ngày mua không hợp lệ.";
        return null;
    }
}

