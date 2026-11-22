$ErrorActionPreference = 'Stop'

function Assert-Contains([string]$Path, [string]$Pattern, [string]$Message) {
    if (-not (Test-Path $Path)) { throw "Missing file: $Path" }
    if ((Get-Content -Raw $Path) -notmatch $Pattern) { throw $Message }
}

$contract = 'src/FitGym.Application/Features/Registrations/IRegistrationService.cs'
$service = 'src/FitGym.Infrastructure/Features/Registrations/RegistrationService.cs'
$controller = 'src/FitGym.Web/Features/Registrations/RegistrationsController.cs'

Assert-Contains $contract 'RegistrationCreateRequest' 'Registration create contract is missing.'
Assert-Contains $contract 'RenewalCreateRequest' 'Renewal contract is missing.'
Assert-Contains $contract 'PaymentStatuses' 'Payment status vocabulary must be centralized.'
Assert-Contains $service 'BeginTransactionAsync' 'Registration/payment writes must be transactional.'
Assert-Contains $service 'package\.GiaTien' 'Total price must come from the selected package.'
Assert-Contains $service 'RegistrationTerms\.Calculate' 'Registration and renewal must use the shared server-side terms calculation.'
& dotnet run --project tests/FitGym.RegistrationRulesTests/FitGym.RegistrationRulesTests.csproj --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Registration terms behavioral tests failed.' }
Assert-Contains $service 'MaYeuCau' 'Every payment must have an idempotency key.'
Assert-Contains $service 'AnyAsync.*MaYeuCau' 'Duplicate payment request keys must be rejected.'
Assert-Contains $service 'PaymentMethods\.Qr' 'QR payments must use an explicit payment method.'
Assert-Contains $service 'PaymentStatuses\.Pending' 'QR payments must remain pending without a provider callback.'
Assert-Contains $service 'DongYSinhTracHoc' 'Biometric consent metadata must be recorded explicitly.'
Assert-Contains $service 'DuLieuKhuonMat.*AsNoTracking' 'Registration detail may expose face-data presence but not its vector.'
if ((Get-Content -Raw $service) -match 'VectorKhuonMat\s*=') { throw 'Registration flow must not fabricate or expose biometric vectors.' }
Assert-Contains $controller 'CurrentEmployeeService' 'Business writes must resolve the authenticated employee.'
Assert-Contains $controller 'ValidateAntiForgeryToken' 'Registration/payment writes must validate anti-forgery.'

foreach ($view in @('Index', 'Create', 'Details', 'Renew')) {
    if (-not (Test-Path "src/FitGym.Web/Views/Registrations/$view.cshtml")) { throw "Missing Registrations/$view view." }
}

'registration/payment contract: PASS'
