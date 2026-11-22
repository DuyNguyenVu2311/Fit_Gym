using FitGym.Application.Features.Equipment;
using FitGym.Infrastructure.Features.Equipment;
using FitGym.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var connectionString = Environment.GetEnvironmentVariable("FITGYM_TEST_CONNECTION")
    ?? throw new InvalidOperationException("FITGYM_TEST_CONNECTION is required.");
var options = new DbContextOptionsBuilder<FitGymDbContext>().UseSqlServer(connectionString).Options;
await using var db = new FitGymDbContext(options);
var employeeId = await db.NhanVien
    .Where(x => x.HoTen == "Test Authorization Employee")
    .Select(x => x.MaNhanVien)
    .SingleAsync();
var service = new EquipmentService(db);

var futureEquipment = await service.CreateAsync(new EquipmentCreateRequest(
    "__FITGYM_INVALID_EQUIPMENT__", "test", DateTime.Today.AddDays(1)));
Ensure(!futureEquipment.Succeeded, "Future equipment purchase date must be rejected.");

var created = await service.CreateAsync(new EquipmentCreateRequest(
    "__FITGYM_EQUIPMENT__", "functional-test", DateTime.Today));
Ensure(created.Succeeded, created.Error ?? "Equipment creation failed.");
var equipmentId = created.Value;

var futureMaintenance = await service.AddMaintenanceAsync(new MaintenanceCreateRequest(
    equipmentId, employeeId, DateTime.Today.AddDays(1), "future", 0));
Ensure(!futureMaintenance.Succeeded, "Future maintenance history must be rejected.");
var negativeCost = await service.AddMaintenanceAsync(new MaintenanceCreateRequest(
    equipmentId, employeeId, DateTime.Today, "negative", -1));
Ensure(!negativeCost.Succeeded, "Negative maintenance cost must be rejected.");
var maintenance = await service.AddMaintenanceAsync(new MaintenanceCreateRequest(
    equipmentId, employeeId, DateTime.Today, "__FITGYM_MAINTENANCE__", 150000));
Ensure(maintenance.Succeeded, maintenance.Error ?? "Maintenance creation failed.");

var detail = await service.GetAsync(equipmentId);
Ensure(detail is not null && detail.MaintenanceHistory.Count == 1 && detail.DaCoLichSuBaoTri,
    "Equipment detail must include maintenance history.");
var updated = await service.UpdateAsync(new EquipmentUpdateRequest(
    equipmentId, "__FITGYM_EQUIPMENT__", "functional-test", DateTime.Today,
    EquipmentStatuses.UnderMaintenance));
Ensure(updated.Succeeded, updated.Error ?? "Equipment status update failed.");
Ensure((await service.GetAsync(equipmentId))?.TrangThai == EquipmentStatuses.UnderMaintenance,
    "Equipment status must persist.");

Console.WriteLine("equipment functional scenarios: PASS");

static void Ensure(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
