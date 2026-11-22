using System.Data;
using FitGym.Application.Common;
using FitGym.Application.Features.Warehouse;
using FitGym.Application.Models;
using FitGym.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitGym.Infrastructure.Features.Warehouse;

public sealed class WarehouseService(FitGymDbContext dbContext) : IWarehouseService
{
    public async Task<IReadOnlyList<GoodsSummary>> SearchGoodsAsync(
        string? search,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.HangHoa.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.TenHang.Contains(term));
        }
        if (GoodsStatuses.All.Contains(status, StringComparer.Ordinal))
            query = query.Where(x => x.TrangThai == status);

        var goods = await query.OrderBy(x => x.TenHang).ToListAsync(cancellationToken);
        var result = new List<GoodsSummary>(goods.Count);
        foreach (var item in goods)
        {
            result.Add(new GoodsSummary(
                item.MaHangHoa, item.TenHang, item.DonViTinh,
                item.GiaBan, item.TrangThai,
                await CalculateStockAsync(item.MaHangHoa, cancellationToken)));
        }
        return result;
    }

    public async Task<GoodsDetail?> GetGoodsAsync(int id, CancellationToken cancellationToken = default)
    {
        var goods = await dbContext.HangHoa.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaHangHoa == id, cancellationToken);
        if (goods is null) return null;
        var isUsed = await dbContext.ChiTietPhieuKho.AsNoTracking()
            .AnyAsync(x => x.MaHangHoa == id, cancellationToken);
        return new GoodsDetail(
            goods.MaHangHoa, goods.TenHang, goods.DonViTinh, goods.GiaBan,
            goods.TrangThai, await CalculateStockAsync(id, cancellationToken), isUsed);
    }

    public async Task<OperationResult<int>> CreateGoodsAsync(
        GoodsCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var error = ValidateGoods(request.TenHang, request.DonViTinh, request.GiaBan);
        if (error is not null) return OperationResult<int>.Failure(error);
        var name = request.TenHang.Trim();
        if (await dbContext.HangHoa.AnyAsync(x => x.TenHang == name, cancellationToken))
            return OperationResult<int>.Failure("Tên hàng hóa đã tồn tại.");
        var goods = new HangHoa
        {
            TenHang = name,
            DonViTinh = request.DonViTinh.Trim(),
            GiaBan = request.GiaBan,
            TrangThai = GoodsStatuses.Active
        };
        dbContext.HangHoa.Add(goods);
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Success(goods.MaHangHoa);
    }

    public async Task<OperationResult> UpdateGoodsAsync(
        GoodsUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var error = ValidateGoods(request.TenHang, request.DonViTinh, request.GiaBan);
        if (error is not null) return OperationResult.Failure(error);
        if (!GoodsStatuses.All.Contains(request.TrangThai, StringComparer.Ordinal))
            return OperationResult.Failure("Trạng thái hàng hóa không hợp lệ.");
        var goods = await dbContext.HangHoa.SingleOrDefaultAsync(x => x.MaHangHoa == request.MaHangHoa, cancellationToken);
        if (goods is null) return OperationResult.Failure("Không tìm thấy hàng hóa.");
        var name = request.TenHang.Trim();
        if (await dbContext.HangHoa.AnyAsync(
                x => x.TenHang == name && x.MaHangHoa != request.MaHangHoa,
                cancellationToken))
            return OperationResult.Failure("Tên hàng hóa đã tồn tại.");
        goods.TenHang = name;
        goods.DonViTinh = request.DonViTinh.Trim();
        goods.GiaBan = request.GiaBan;
        goods.TrangThai = request.TrangThai;
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<IReadOnlyList<WarehouseStockItem>> GetStockAsync(CancellationToken cancellationToken = default)
    {
        var goods = await SearchGoodsAsync(null, null, cancellationToken);
        return goods.Select(x => new WarehouseStockItem(x.MaHangHoa, x.TenHang, x.DonViTinh, x.TonKho)).ToList();
    }

    public async Task<IReadOnlyList<WarehouseDocumentSummary>> ListDocumentsAsync(
        string? type,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.PhieuNhapXuatKho.AsNoTracking();
        if (DocumentTypes.All.Contains(type, StringComparer.Ordinal))
            query = query.Where(x => x.LoaiPhieu == type);
        if (status is DocumentStatuses.Draft or DocumentStatuses.Confirmed)
            query = query.Where(x => x.TrangThai == status);

        return await query.OrderByDescending(x => x.MaPhieu)
            .Select(x => new WarehouseDocumentSummary(
                x.MaPhieu,
                x.LoaiPhieu,
                x.NgayLap,
                x.TrangThai,
                x.GhiChu,
                dbContext.ChiTietPhieuKho.Count(line => line.MaPhieu == x.MaPhieu),
                dbContext.ChiTietPhieuKho
                    .Where(line => line.MaPhieu == x.MaPhieu)
                    .Sum(line => (decimal?)(line.SoLuong * line.DonGia)) ?? 0))
            .ToListAsync(cancellationToken);
    }

    public async Task<WarehouseDocumentDetail?> GetDocumentAsync(int id, CancellationToken cancellationToken = default)
    {
        var document = await dbContext.PhieuNhapXuatKho.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaPhieu == id, cancellationToken);
        if (document is null) return null;
        var lines = await (
            from line in dbContext.ChiTietPhieuKho.AsNoTracking()
            join goods in dbContext.HangHoa.AsNoTracking() on line.MaHangHoa equals goods.MaHangHoa
            where line.MaPhieu == id
            orderby goods.TenHang
            select new WarehouseDocumentLine(
                line.MaChiTiet, line.MaHangHoa, goods.TenHang, goods.DonViTinh,
                line.SoLuong, line.DonGia, line.SoLuong * line.DonGia))
            .ToListAsync(cancellationToken);
        return new WarehouseDocumentDetail(
            document.MaPhieu, document.MaNhanVien, document.LoaiPhieu,
            document.NgayLap, document.TrangThai, document.NgayXacNhan,
            document.MaNguoiXacNhan, document.GhiChu, lines,
            lines.Sum(x => x.ThanhTien));
    }

    public async Task<OperationResult<int>> CreateDocumentAsync(
        WarehouseDocumentCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!DocumentTypes.All.Contains(request.LoaiPhieu, StringComparer.Ordinal))
            return OperationResult<int>.Failure("Loại phiếu không hợp lệ.");
        if (!await IsActiveEmployeeAsync(request.MaNhanVien, cancellationToken))
            return OperationResult<int>.Failure("Nhân viên lập phiếu không hợp lệ.");
        var document = new PhieuNhapXuatKho
        {
            MaNhanVien = request.MaNhanVien,
            LoaiPhieu = request.LoaiPhieu,
            NgayLap = DateTime.Now,
            TrangThai = DocumentStatuses.Draft,
            GhiChu = NormalizeOptional(request.GhiChu)
        };
        dbContext.PhieuNhapXuatKho.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Success(document.MaPhieu);
    }

    public async Task<OperationResult> AddOrUpdateLineAsync(
        WarehouseLineRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.SoLuong <= 0) return OperationResult.Failure("Số lượng phải lớn hơn 0.");
        if (request.DonGia < 0) return OperationResult.Failure("Đơn giá không thể âm.");
        var document = await dbContext.PhieuNhapXuatKho.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaPhieu == request.MaPhieu, cancellationToken);
        if (document is null || document.TrangThai != DocumentStatuses.Draft)
            return OperationResult.Failure("Chỉ có thể sửa phiếu nháp.");
        var goods = await dbContext.HangHoa.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaHangHoa == request.MaHangHoa, cancellationToken);
        if (goods is null || goods.TrangThai != GoodsStatuses.Active)
            return OperationResult.Failure("Hàng hóa không tồn tại hoặc đã ngừng hoạt động.");

        var line = await dbContext.ChiTietPhieuKho.SingleOrDefaultAsync(
            x => x.MaPhieu == request.MaPhieu && x.MaHangHoa == request.MaHangHoa,
            cancellationToken);
        if (line is null)
        {
            dbContext.ChiTietPhieuKho.Add(new ChiTietPhieuKho
            {
                MaPhieu = request.MaPhieu,
                MaHangHoa = request.MaHangHoa,
                SoLuong = request.SoLuong,
                DonGia = request.DonGia
            });
        }
        else
        {
            line.SoLuong = request.SoLuong;
            line.DonGia = request.DonGia;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> RemoveLineAsync(
        int documentId,
        int lineId,
        CancellationToken cancellationToken = default)
    {
        var line = await dbContext.ChiTietPhieuKho
            .SingleOrDefaultAsync(x => x.MaChiTiet == lineId && x.MaPhieu == documentId, cancellationToken);
        if (line is null) return OperationResult.Failure("Không tìm thấy dòng phiếu.");
        var isDraft = await dbContext.PhieuNhapXuatKho.AsNoTracking()
            .AnyAsync(x => x.MaPhieu == documentId && x.TrangThai == DocumentStatuses.Draft, cancellationToken);
        if (!isDraft) return OperationResult.Failure("Chỉ có thể sửa phiếu nháp.");
        dbContext.ChiTietPhieuKho.Remove(line);
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<OperationResult> ConfirmAsync(
        int documentId,
        int confirmingEmployeeId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        if (!await IsActiveEmployeeAsync(confirmingEmployeeId, cancellationToken))
            return OperationResult.Failure("Nhân viên xác nhận không hợp lệ.");

        var claimed = await dbContext.PhieuNhapXuatKho
            .Where(x => x.MaPhieu == documentId && x.TrangThai == DocumentStatuses.Draft)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.TrangThai, DocumentStatuses.Confirming), cancellationToken);
        if (claimed == 0)
        {
            var currentStatus = await dbContext.PhieuNhapXuatKho.AsNoTracking()
                .Where(x => x.MaPhieu == documentId)
                .Select(x => x.TrangThai)
                .SingleOrDefaultAsync(cancellationToken);
            return currentStatus switch
            {
                null => OperationResult.Failure("Không tìm thấy phiếu kho."),
                DocumentStatuses.Confirmed => OperationResult.Success(),
                _ => OperationResult.Failure("Phiếu đang được xác nhận bởi thao tác khác.")
            };
        }

        var document = await dbContext.PhieuNhapXuatKho.AsNoTracking()
            .SingleAsync(x => x.MaPhieu == documentId, cancellationToken);

        var lines = await dbContext.ChiTietPhieuKho.AsNoTracking()
            .Where(x => x.MaPhieu == documentId)
            .ToListAsync(cancellationToken);
        if (lines.Count == 0) return OperationResult.Failure("Phiếu kho phải có ít nhất một dòng hàng.");
        if (document.LoaiPhieu == DocumentTypes.Outbound)
        {
            foreach (var line in lines)
            {
                var available = await CalculateStockAsync(line.MaHangHoa, cancellationToken);
                if (available < line.SoLuong)
                    return OperationResult.Failure($"Tồn kho không đủ cho hàng hóa mã {line.MaHangHoa}.");
            }
        }

        var now = DateTime.Now;
        var updated = await dbContext.PhieuNhapXuatKho
            .Where(x => x.MaPhieu == documentId && x.TrangThai == DocumentStatuses.Confirming)
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.TrangThai, DocumentStatuses.Confirmed)
                    .SetProperty(x => x.NgayXacNhan, now)
                    .SetProperty(x => x.MaNguoiXacNhan, confirmingEmployeeId),
                cancellationToken);
        if (updated != 1) return OperationResult.Failure("Không thể hoàn tất xác nhận phiếu.");

        await transaction.CommitAsync(cancellationToken);
        return OperationResult.Success();
    }

    private async Task<int> CalculateStockAsync(int goodsId, CancellationToken cancellationToken)
    {
        var inbound = await (
            from line in dbContext.ChiTietPhieuKho.AsNoTracking()
            join document in dbContext.PhieuNhapXuatKho.AsNoTracking() on line.MaPhieu equals document.MaPhieu
            where line.MaHangHoa == goodsId &&
                  document.TrangThai == DocumentStatuses.Confirmed &&
                  document.LoaiPhieu == DocumentTypes.Inbound
            select (int?)line.SoLuong).SumAsync(cancellationToken) ?? 0;
        var outbound = await (
            from line in dbContext.ChiTietPhieuKho.AsNoTracking()
            join document in dbContext.PhieuNhapXuatKho.AsNoTracking() on line.MaPhieu equals document.MaPhieu
            where line.MaHangHoa == goodsId &&
                  document.TrangThai == DocumentStatuses.Confirmed &&
                  document.LoaiPhieu == DocumentTypes.Outbound
            select (int?)line.SoLuong).SumAsync(cancellationToken) ?? 0;
        return inbound - outbound;
    }

    private Task<bool> IsActiveEmployeeAsync(int id, CancellationToken cancellationToken) =>
        dbContext.NhanVien.AsNoTracking().AnyAsync(
            x => x.MaNhanVien == id && x.TrangThai == GoodsStatuses.Active,
            cancellationToken);

    private static string? ValidateGoods(string name, string unit, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Vui lòng nhập tên hàng hóa.";
        if (string.IsNullOrWhiteSpace(unit)) return "Vui lòng nhập đơn vị tính.";
        if (price < 0) return "Giá bán không thể âm.";
        return null;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

