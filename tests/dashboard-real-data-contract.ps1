$ErrorActionPreference = 'Stop'

$controller = Get-Content -Raw "$PSScriptRoot\..\src\FitGym.Web\Controllers\HomeController.cs"
$view = Get-Content -Raw "$PSScriptRoot\..\src\FitGym.Web\Views\Home\Index.cshtml"
$view += Get-Content -Raw "$PSScriptRoot\..\src\FitGym.Web\Views\Home\_OperationsSnapshot.cshtml"
$layout = Get-Content -Raw "$PSScriptRoot\..\src\FitGym.Web\Views\Shared\_Layout.cshtml"
$publicPrototype = "$PSScriptRoot\..\src\FitGym.Web\wwwroot\fit-gym-dashboard.html"

if ($controller -match 'PhysicalFile|fit-gym-dashboard\.html') {
    throw 'Home must render the authenticated MVC view, not a static prototype.'
}
if (Test-Path -LiteralPath $publicPrototype) {
    throw 'The fake-data dashboard prototype must not be publicly served from wwwroot.'
}
if ($view -match '342|G-01|Quận 3|Dữ liệu mẫu') {
    throw 'The operational home view must not contain fabricated KPI, gate, or branch data.'
}
foreach ($route in @('Members', 'Packages', 'Registrations', 'CheckIns', 'Goods', 'Equipment')) {
    if ($view -notmatch ('asp-controller="' + [regex]::Escape($route) + '"') -and -not $layout.Contains('("' + $route + '",')) {
        throw "Home view is missing the real $route workflow."
    }
}

Write-Host 'real-data dashboard contract: PASS'
