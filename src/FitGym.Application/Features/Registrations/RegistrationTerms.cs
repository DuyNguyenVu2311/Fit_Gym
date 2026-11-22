namespace FitGym.Application.Features.Registrations;

public sealed record RegistrationTerms(DateTime StartDate, DateTime EndDate, decimal Total)
{
    public static RegistrationTerms Calculate(DateTime startDate, int durationMonths, decimal price)
    {
        if (startDate == default) throw new ArgumentException("Vui lòng chọn ngày bắt đầu.", nameof(startDate));
        if (durationMonths is < 1 or > 120)
            throw new ArgumentOutOfRangeException(nameof(durationMonths), "Thời hạn gói tập phải từ 1 đến 120 tháng.");
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price), "Giá gói tập không thể âm.");
        try
        {
            return new(startDate.Date, startDate.Date.AddMonths(durationMonths), price);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new ArgumentException("Ngày kết thúc vượt quá phạm vi hỗ trợ. Vui lòng chọn ngày bắt đầu khác.", nameof(startDate));
        }
    }
}
