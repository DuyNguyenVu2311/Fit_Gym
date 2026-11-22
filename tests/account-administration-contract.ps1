$ErrorActionPreference = 'Stop'

function Assert-Contains {
    param([string]$Path, [string]$Pattern, [string]$Message)
    if (-not (Test-Path $Path)) { throw "Missing file: $Path" }
    $content = Get-Content -Raw $Path
    if ($content -notmatch $Pattern) { throw $Message }
}

$controller = 'src/FitGym.Web/Features/Accounts/AccountsController.cs'
$models = 'src/FitGym.Web/Features/Accounts/AccountViewModels.cs'
$service = 'src/FitGym.Web/Features/Accounts/AccountAdministrationService.cs'

Assert-Contains $controller 'Authorize\(Roles\s*=\s*FitGymRoles\.QuanLy\)' 'Accounts controller must be manager-only.'
Assert-Contains $controller 'ValidateAntiForgeryToken' 'Every mutating account action must require anti-forgery.'
Assert-Contains $controller 'User\.Identity\?\.Name' 'Self-protection must use the authenticated account.'
Assert-Contains $controller 'ResetPassword' 'A reset-password workflow is required.'
Assert-Contains $service 'BeginTransactionAsync' 'Account and employee creation must be transactional.'
Assert-Contains $service 'FindByNameAsync' 'Duplicate usernames must be checked before creation.'
Assert-Contains $service 'FitGymRoles\.All\.Contains' 'Only the two approved roles may be assigned.'
Assert-Contains $service 'CannotDisableCurrentUser' 'A manager must not deactivate their own account.'
Assert-Contains $service 'ResetPasswordAsync' 'Password reset must use Identity APIs and never expose stored passwords.'
Assert-Contains $models '\[Required' 'Required account fields must have server-side validation.'
if ((Get-Content -Raw src/FitGym.Web/Views/Accounts/Create.cshtml) -match 'asp-for="Email"') { throw 'Staff account creation must not require email.' }
Assert-Contains $models '\[DataType\(DataType\.Password\)\]' 'Passwords must render as password inputs.'

foreach ($view in @('Index', 'Create', 'Edit', 'ResetPassword')) {
    if (-not (Test-Path "src/FitGym.Web/Views/Accounts/$view.cshtml")) {
        throw "Missing Accounts/$view view."
    }
}

$allViews = Get-Content -Raw src/FitGym.Web/Views/Accounts/*.cshtml
if ($allViews -match 'PasswordHash') { throw 'Views must never display password hashes.' }

'account administration contract: PASS'
