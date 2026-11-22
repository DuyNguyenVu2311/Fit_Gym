namespace FitGym.Application.Security;

public static class FitGymRoles
{
    public const string QuanLy = "QuanLy";
    public const string NhanVien = "NhanVien";
    public const string AllCsv = QuanLy + "," + NhanVien;

    public static readonly string[] All = [QuanLy, NhanVien];
}
