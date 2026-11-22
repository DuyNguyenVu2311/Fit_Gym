using System.Globalization;

namespace FitGym.Web.Features.Shared;

public static class VndMoney
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string Format(decimal amount) => amount.ToString("#,##0.##", Vietnamese) + " VND";
}
