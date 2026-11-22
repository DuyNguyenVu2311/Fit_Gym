using FitGym.Application.Features.Warehouse;
using FitGym.Infrastructure.Features.Warehouse;
using FitGym.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var connectionString = Environment.GetEnvironmentVariable("FITGYM_TEST_CONNECTION")
    ?? throw new InvalidOperationException("FITGYM_TEST_CONNECTION is required.");

FitGymDbContext CreateDb() => new(new DbContextOptionsBuilder<FitGymDbContext>()
    .UseSqlServer(connectionString)
    .Options);

await using var db = CreateDb();
var employeeId = await db.NhanVien
    .Where(x => x.HoTen == "Test Authorization Employee")
    .Select(x => x.MaNhanVien)
    .SingleAsync();
var service = new WarehouseService(db);

var goodsResult = await service.CreateGoodsAsync(new GoodsCreateRequest(
    "__FITGYM_WAREHOUSE_GOODS__", "unit", 10000));
Ensure(goodsResult.Succeeded, goodsResult.Error ?? "Goods creation failed.");
var goodsId = goodsResult.Value;

var inboundResult = await service.CreateDocumentAsync(new WarehouseDocumentCreateRequest(
    employeeId, DocumentTypes.Inbound, "functional-test"));
Ensure(inboundResult.Succeeded, inboundResult.Error ?? "Inbound draft creation failed.");
var inboundId = inboundResult.Value;
Ensure((await service.AddOrUpdateLineAsync(new WarehouseLineRequest(inboundId, goodsId, 10, 5000))).Succeeded,
    "Inbound line creation failed.");
Ensure((await service.GetGoodsAsync(goodsId))?.TonKho == 0, "Draft inbound document must not affect stock.");
Ensure((await service.ConfirmAsync(inboundId, employeeId)).Succeeded, "Inbound confirmation failed.");
Ensure((await service.ConfirmAsync(inboundId, employeeId)).Succeeded, "Repeated confirmation must be idempotent.");
Ensure((await service.GetGoodsAsync(goodsId))?.TonKho == 10, "Confirmed inbound stock must equal 10.");

var outboundResult = await service.CreateDocumentAsync(new WarehouseDocumentCreateRequest(
    employeeId, DocumentTypes.Outbound, "functional-test"));
Ensure(outboundResult.Succeeded, outboundResult.Error ?? "Outbound draft creation failed.");
var outboundId = outboundResult.Value;
Ensure((await service.AddOrUpdateLineAsync(new WarehouseLineRequest(outboundId, goodsId, 11, 10000))).Succeeded,
    "Outbound line creation failed.");
var insufficient = await service.ConfirmAsync(outboundId, employeeId);
Ensure(!insufficient.Succeeded && insufficient.Error?.Contains("không đủ", StringComparison.OrdinalIgnoreCase) == true,
    "Outbound quantity greater than stock must be rejected.");
Ensure((await service.GetGoodsAsync(goodsId))?.TonKho == 10, "Rejected outbound must not change stock.");

Ensure((await service.AddOrUpdateLineAsync(new WarehouseLineRequest(outboundId, goodsId, 4, 10000))).Succeeded,
    "Outbound line correction failed.");
Ensure((await service.ConfirmAsync(outboundId, employeeId)).Succeeded, "Valid outbound confirmation failed.");
Ensure((await service.GetGoodsAsync(goodsId))?.TonKho == 6, "Derived stock must equal inbound 10 minus outbound 4.");

var concurrentResult = await service.CreateDocumentAsync(new WarehouseDocumentCreateRequest(
    employeeId, DocumentTypes.Inbound, "concurrency-test"));
Ensure(concurrentResult.Succeeded, concurrentResult.Error ?? "Concurrent draft creation failed.");
var concurrentId = concurrentResult.Value;
Ensure((await service.AddOrUpdateLineAsync(new WarehouseLineRequest(concurrentId, goodsId, 2, 5000))).Succeeded,
    "Concurrent test line creation failed.");

await using var db1 = CreateDb();
await using var db2 = CreateDb();
var confirmations = await Task.WhenAll(
    new WarehouseService(db1).ConfirmAsync(concurrentId, employeeId),
    new WarehouseService(db2).ConfirmAsync(concurrentId, employeeId));
Ensure(confirmations.All(x => x.Succeeded), "Concurrent confirmation calls must both return idempotent success.");

await using var verifyDb = CreateDb();
var verifyService = new WarehouseService(verifyDb);
Ensure((await verifyService.GetGoodsAsync(goodsId))?.TonKho == 8,
    "Concurrent confirmation must apply the document exactly once.");

Console.WriteLine("warehouse functional scenarios: PASS");

static void Ensure(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
