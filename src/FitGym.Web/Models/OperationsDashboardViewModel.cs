using FitGym.Application.Features.CheckIns;

namespace FitGym.Web.Models;

public sealed class OperationsDashboardViewModel
{
    public DateTime Date { get; init; }
    public bool TrackToday { get; init; }
    public bool EmployeeProfileMissing { get; init; }
    public int CurrentAccountId { get; init; }
    public int Total { get; init; }
    public int Successful { get; init; }
    public int FaceFailures { get; init; }
    public int Expiring { get; init; }
    public int[] Hourly { get; init; } = new int[24];
    public IReadOnlyList<CheckInHistoryItem> Recent { get; init; } = [];
    public IReadOnlyList<CheckInHistoryItem> Failed { get; init; } = [];
}
