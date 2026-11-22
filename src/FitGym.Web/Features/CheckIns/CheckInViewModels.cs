using FitGym.Application.Features.CheckIns;

namespace FitGym.Web.Features.CheckIns;

public sealed class CheckInIndexViewModel
{
    public DateTime Date { get; init; }
    public string? Search { get; init; }
    public IReadOnlyList<CheckInHistoryItem> History { get; init; } = [];
}


