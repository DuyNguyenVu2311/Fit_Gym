$ErrorActionPreference = 'Stop'

function Assert-Contains([string]$Path, [string]$Pattern, [string]$Message) {
    if (-not (Test-Path $Path)) { throw "Missing file: $Path" }
    if ((Get-Content -Raw $Path) -notmatch $Pattern) { throw $Message }
}

$memberContract = 'src/FitGym.Application/Features/Members/IMemberService.cs'
$memberService = 'src/FitGym.Infrastructure/Features/Members/MemberService.cs'
$memberController = 'src/FitGym.Web/Features/Members/MembersController.cs'
$packageContract = 'src/FitGym.Application/Features/Packages/IPackageService.cs'
$packageService = 'src/FitGym.Infrastructure/Features/Packages/PackageService.cs'
$packageController = 'src/FitGym.Web/Features/Packages/PackagesController.cs'

Assert-Contains $memberContract 'MemberCreateRequest' 'Member create contract is missing.'
Assert-Contains $memberContract 'MemberDetail' 'Member detail contract is missing.'
Assert-Contains $memberService 'AnyAsync.*SoDienThoai' 'Duplicate member phone numbers must be rejected.'
Assert-Contains $memberService 'AsNoTracking' 'Read-only member queries must not track entities.'
Assert-Contains $memberService 'NgungHoatDong' 'Members must be deactivated instead of hard-deleted.'
Assert-Contains $memberController 'Authorize\(Roles\s*=\s*FitGymRoles\.AllCsv\)' 'Employees and managers must be able to manage members.'
Assert-Contains $memberController 'ValidateAntiForgeryToken' 'Member changes must validate anti-forgery tokens.'

Assert-Contains $packageContract 'PackageCreateRequest' 'Package create contract is missing.'
Assert-Contains $packageService 'ThoiHanThang\s*<=\s*0' 'Package duration must be positive.'
Assert-Contains $packageService 'GiaTien\s*<\s*0' 'Package price cannot be negative.'
Assert-Contains $packageService 'DangKyGoiTap\.AnyAsync' 'Used packages must never be hard-deleted.'
Assert-Contains $packageController 'Authorize\(Roles\s*=\s*FitGymRoles\.QuanLy\)' 'Package mutations must be manager-only.'
Assert-Contains $packageController 'ValidateAntiForgeryToken' 'Package changes must validate anti-forgery tokens.'

foreach ($view in @(
    'Views/Members/Index.cshtml', 'Views/Members/Details.cshtml', 'Views/Members/Create.cshtml', 'Views/Members/Edit.cshtml',
    'Views/Packages/Index.cshtml', 'Views/Packages/Details.cshtml', 'Views/Packages/Create.cshtml', 'Views/Packages/Edit.cshtml')) {
    if (-not (Test-Path "src/FitGym.Web/$view")) { throw "Missing view: $view" }
}

'member/package contract: PASS'
