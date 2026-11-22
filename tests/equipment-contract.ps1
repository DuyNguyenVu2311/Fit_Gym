$ErrorActionPreference = 'Stop'

function Assert-Contains([string]$Path, [string]$Pattern, [string]$Message) {
    if (-not (Test-Path $Path)) { throw "Missing file: $Path" }
    if ((Get-Content -Raw $Path) -notmatch $Pattern) { throw $Message }
}

$contract = 'src/FitGym.Application/Features/Equipment/IEquipmentService.cs'
$service = 'src/FitGym.Infrastructure/Features/Equipment/EquipmentService.cs'
$controller = 'src/FitGym.Web/Features/Equipment/EquipmentController.cs'

Assert-Contains $contract 'EquipmentCreateRequest' 'Equipment create contract is missing.'
Assert-Contains $contract 'MaintenanceCreateRequest' 'Maintenance create contract is missing.'
Assert-Contains $service 'ChiPhi\s*<\s*0' 'Maintenance cost cannot be negative.'
Assert-Contains $service 'NgayBaoTri\.Date\s*>\s*DateTime\.Today' 'Maintenance history cannot be future-dated.'
Assert-Contains $service 'AnyAsync.*MaThietBi' 'Equipment usage/history must be checked instead of hard delete.'
if ((Get-Content -Raw $service) -match 'ChiNhanh|ViTri|BaoHanh|Telemetry|CamBien') { throw 'Equipment service contains unsupported fields.' }
Assert-Contains $controller 'Authorize\(Roles\s*=\s*FitGymRoles\.QuanLy\)' 'Equipment mutations must be manager-only.'
Assert-Contains $controller 'CurrentEmployeeService' 'Maintenance entries must resolve the current employee.'
Assert-Contains $controller 'ValidateAntiForgeryToken' 'Equipment changes must validate anti-forgery.'

foreach ($view in @('Index', 'Details', 'Create', 'Edit', 'AddMaintenance')) {
    if (-not (Test-Path "src/FitGym.Web/Views/Equipment/$view.cshtml")) { throw "Missing Equipment/$view view." }
}

'equipment contract: PASS'
