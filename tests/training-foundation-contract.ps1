$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$required = @(
    'src\FitGym.Application\Features\Training\ITrainingFoundationService.cs',
    'src\FitGym.Infrastructure\Features\Training\TrainingFoundationService.cs',
    'src\FitGym.Web\Features\Training\TrainingController.cs',
    'src\FitGym.Web\Features\Training\TrainingViewModels.cs',
    'src\FitGym.Web\Views\Training\Profiles.cshtml',
    'src\FitGym.Web\Views\Training\EditProfile.cshtml',
    'src\FitGym.Web\Views\Training\Exercises.cshtml',
    'src\FitGym.Web\Views\Training\ExerciseForm.cshtml'
)
foreach ($relative in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $root $relative))) {
        throw "Missing training foundation file: $relative"
    }
}

$service = Get-Content -Raw -Encoding UTF8 (Join-Path $root $required[1])
$controller = Get-Content -Raw -Encoding UTF8 (Join-Path $root $required[2])
$di = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'src\FitGym.Infrastructure\DependencyInjection.cs')

foreach ($rule in @('SoNgayTuan is < 1 or > 7', 'SoPhutBuoi <= 0', 'HoiVien', 'BaiTap')) {
    if (-not $service.Contains($rule)) { throw "Training service is missing rule: $rule" }
}
if ($controller -notmatch 'FitGymRoles\.QuanLy' -or $controller -notmatch 'ValidateAntiForgeryToken') {
    throw 'Exercise mutation must be manager-only and anti-forgery protected.'
}
if ($di -notmatch 'ITrainingFoundationService, TrainingFoundationService') {
    throw 'Training foundation service is not registered.'
}

Write-Host 'training foundation contract: PASS'
