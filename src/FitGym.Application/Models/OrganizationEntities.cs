namespace FitGym.Application.Models;

// One operator per independently deployed customer database; this is not a SaaS tenant table.
public sealed class DonViSuDung
{
    public int MaDonVi { get; set; } = 1;
    public string TenDonVi { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public string? Email { get; set; }
    public byte[]? Logo { get; set; }
    public string? LogoContentType { get; set; }
}

public sealed class CoSo
{
    public int MaCoSo { get; set; }
    public int MaDonVi { get; set; } = 1;
    public string TenCoSo { get; set; } = string.Empty;
    public string DiaChi { get; set; } = string.Empty;
}
