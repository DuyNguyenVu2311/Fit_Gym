$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$controllerPath = Join-Path $repoRoot 'src\FitGym.Web\Controllers\HomeController.cs'
$viewPath = Join-Path $repoRoot 'src\FitGym.Web\Views\Home\Index.cshtml'
$publicPrototype = Join-Path $repoRoot 'src\FitGym.Web\wwwroot\fit-gym-dashboard.html'

$controller = Get-Content -Raw -Encoding UTF8 -LiteralPath $controllerPath
$view = Get-Content -Raw -Encoding UTF8 -LiteralPath $viewPath
$view += Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repoRoot 'src\FitGym.Web\Views\Home\_OperationsSnapshot.cshtml')
$layout = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repoRoot 'src\FitGym.Web\Views\Shared\_Layout.cshtml')

if ($controller -notmatch 'Authorize\(Roles.*FitGymRoles.AllCsv' -or $controller -notmatch 'return View\(await LoadAsync' -or $controller -notmatch 'PartialView\("_OperationsSnapshot", await LoadAsync') {
    throw 'The default dashboard must be an authenticated MVC view.'
}
if (Test-Path -LiteralPath $publicPrototype) {
    throw 'A fake-data dashboard must not be served publicly from wwwroot.'
}
foreach ($controllerName in @('Members', 'Packages', 'Registrations', 'CheckIns', 'Goods', 'Equipment')) {
    $fragment = 'asp-controller="' + $controllerName + '"'
    if (-not $view.Contains($fragment) -and -not $layout.Contains('("' + $controllerName + '",')) {
        throw "Operational dashboard is missing the $controllerName workflow."
    }
}
if ($view -match 'G-01|342|Quận 3|Dữ liệu mẫu') {
    throw 'Operational dashboard contains unsupported sample gate, KPI, or branch data.'
}

Write-Output 'Fit_Gym MVC dashboard contract passed.'
