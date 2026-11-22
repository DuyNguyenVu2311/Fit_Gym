$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$failures = [System.Collections.Generic.List[string]]::new()

function Require-File([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $failures.Add("Missing file: $relativePath")
        return $null
    }
    return Get-Content -Raw -LiteralPath $path
}

function Require-Pattern([string]$content, [string]$pattern, [string]$message) {
    if ($null -eq $content -or $content -notmatch $pattern) {
        $failures.Add($message)
    }
}

$roles = Require-File 'src/FitGym.Application/Security/FitGymRoles.cs'
Require-Pattern $roles 'const\s+string\s+QuanLy\s*=\s*"QuanLy"' 'QuanLy role constant is missing.'
Require-Pattern $roles 'const\s+string\s+NhanVien\s*=\s*"NhanVien"' 'NhanVien role constant is missing.'

$initializer = Require-File 'src/FitGym.Infrastructure/Identity/FitGymIdentityInitializer.cs'
Require-Pattern $initializer 'FITGYM_BOOTSTRAP_ADMIN_USER' 'Bootstrap username must come from an environment variable.'
Require-Pattern $initializer 'FITGYM_BOOTSTRAP_ADMIN_PASSWORD' 'Bootstrap password must come from an environment variable.'
Require-Pattern $initializer 'bool\s+isDevelopment' 'Initializer must receive an explicit Development-only flag.'
Require-Pattern $initializer 'if\s*\(!isDevelopment\)' 'Bootstrap account creation must be Development-only.'
Require-Pattern $initializer 'RoleManager<IdentityRole<int>>' 'Identity initializer must seed roles through RoleManager.'

$infrastructure = Require-File 'src/FitGym.Infrastructure/DependencyInjection.cs'
Require-Pattern $infrastructure '\.AddSignInManager\(\)' 'Identity must register SignInManager.'

$program = Require-File 'src/FitGym.Web/Program.cs'
Require-Pattern $program 'AddAuthentication' 'Web authentication services are missing.'
Require-Pattern $program 'ConfigureApplicationCookie' 'Application cookie configuration is missing.'
Require-Pattern $program 'OnValidatePrincipal' 'Active sessions must be rejected after an account is disabled.'
Require-Pattern $program 'app\.Environment\.IsDevelopment\(\)' 'Web must pass the Development-only flag to the identity initializer.'
Require-Pattern $program 'UseAuthentication\(\)' 'Authentication middleware is missing.'
if ($program) {
    $authenticationIndex = $program.IndexOf('UseAuthentication()')
    $authorizationIndex = $program.IndexOf('UseAuthorization()')
    if ($authenticationIndex -lt 0 -or $authorizationIndex -lt 0 -or $authenticationIndex -gt $authorizationIndex) {
        $failures.Add('UseAuthentication must execute before UseAuthorization.')
    }
}

$controller = Require-File 'src/FitGym.Web/Controllers/AccountController.cs'
Require-Pattern $controller '\[AllowAnonymous\]' 'Login must explicitly allow anonymous access.'
Require-Pattern $controller '\[ValidateAntiForgeryToken\]' 'Account POST actions must validate anti-forgery tokens.'
Require-Pattern $controller 'TrangThai\s*!=\s*"HoatDong"' 'Login must reject inactive accounts.'
Require-Pattern $controller 'SignOutAsync' 'Logout must call SignOutAsync.'

$loginModel = Require-File 'src/FitGym.Web/Models/Account/LoginViewModel.cs'
Require-Pattern $loginModel '\[Required' 'Login fields must use required validation.'

$homeController = Require-File 'src/FitGym.Web/Controllers/HomeController.cs'
Require-Pattern $homeController '\[Authorize\(Roles\s*=\s*.*FitGymRoles\.AllCsv\)\]' 'The dashboard must require a staff/manager role, not just any signed-in account.'

$loginView = Require-File 'src/FitGym.Web/Views/Account/Login.cshtml'
Require-Pattern $loginView 'asp-validation-summary' 'Login view must display validation errors.'
$accessDeniedView = Require-File 'src/FitGym.Web/Views/Account/AccessDenied.cshtml'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Output "FAIL: $_" }
    exit 1
}

Write-Output 'authentication contract: PASS'
