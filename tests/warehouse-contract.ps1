$ErrorActionPreference = 'Stop'

function Assert-Contains([string]$Path, [string]$Pattern, [string]$Message) {
    if (-not (Test-Path $Path)) { throw "Missing file: $Path" }
    if ((Get-Content -Raw $Path) -notmatch $Pattern) { throw $Message }
}

$contract = 'src/FitGym.Application/Features/Warehouse/IWarehouseService.cs'
$service = 'src/FitGym.Infrastructure/Features/Warehouse/WarehouseService.cs'
$goodsController = 'src/FitGym.Web/Features/Warehouse/GoodsController.cs'
$documentsController = 'src/FitGym.Web/Features/Warehouse/WarehouseDocumentsController.cs'

Assert-Contains $contract 'GoodsCreateRequest' 'Goods create contract is missing.'
Assert-Contains $contract 'WarehouseDocumentCreateRequest' 'Warehouse document contract is missing.'
Assert-Contains $contract 'WarehouseStockItem' 'Derived stock contract is missing.'
Assert-Contains $service 'IsolationLevel\.Serializable' 'Confirmation must use a serializable transaction.'
Assert-Contains $service 'DocumentStatuses\.Confirmed' 'Only confirmed documents may affect stock.'
Assert-Contains $service 'DocumentTypes\.Inbound' 'Inbound stock calculation is missing.'
Assert-Contains $service 'DocumentTypes\.Outbound' 'Outbound stock calculation is missing.'
Assert-Contains $service 'ExecuteUpdateAsync' 'Confirmation must use an idempotent conditional update.'
Assert-Contains $service 'available\s*<\s*line\.SoLuong' 'Outbound confirmation must reject insufficient stock.'
Assert-Contains $service 'AnyAsync.*MaHangHoa' 'Used goods must be deactivated instead of hard-deleted.'
if ((Get-Content -Raw $service) -match 'NhaCungCap|TonToiThieu|ChiNhanh|BaoHanh') { throw 'Warehouse service contains unsupported fields.' }
Assert-Contains $goodsController 'Authorize\(Roles\s*=\s*FitGymRoles\.QuanLy\)' 'Goods mutations must be manager-only.'
Assert-Contains $documentsController 'ValidateAntiForgeryToken' 'Warehouse mutations must validate anti-forgery.'
Assert-Contains $documentsController 'CurrentEmployeeService' 'Document writes must resolve the current employee.'

foreach ($view in @(
    'Views/Goods/Index.cshtml', 'Views/Goods/Create.cshtml', 'Views/Goods/Edit.cshtml',
    'Views/WarehouseDocuments/Index.cshtml', 'Views/WarehouseDocuments/Create.cshtml', 'Views/WarehouseDocuments/Details.cshtml')) {
    if (-not (Test-Path "src/FitGym.Web/$view")) { throw "Missing view: $view" }
}

'warehouse contract: PASS'
