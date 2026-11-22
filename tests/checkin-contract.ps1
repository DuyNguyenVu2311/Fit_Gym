$ErrorActionPreference = 'Stop'

function Assert-Contains([string]$Path, [string]$Pattern, [string]$Message) {
    if (-not (Test-Path $Path)) { throw "Missing file: $Path" }
    if ((Get-Content -Raw $Path) -notmatch $Pattern) { throw $Message }
}

$contract = 'src/FitGym.Application/Features/CheckIns/ICheckInService.cs'
$service = 'src/FitGym.Infrastructure/Features/CheckIns/CheckInService.cs'
$controller = 'src/FitGym.Web/Features/CheckIns/CheckInsController.cs'

Assert-Contains $contract 'RecordFaceAttemptAsync' 'Face-attempt workflow is missing.'
Assert-Contains $contract 'RecordGateEntryAsync' 'Device-independent entry event workflow is missing.'
Assert-Contains $contract 'RecordManualAsync' 'Manual check-in workflow is missing.'
Assert-Contains $contract 'RequiresStaffAssistance' 'Three failed attempts must escalate to staff.'
Assert-Contains $service 'RegistrationValidity\.Evaluate' 'Check-in must use the shared paid-period validity rule.'
Assert-Contains 'src/FitGym.Application/Features/Registrations/RegistrationValidity.cs' 'startDate.Date > today.Date' 'Check-in must validate the membership start date.'
Assert-Contains 'src/FitGym.Application/Features/Registrations/RegistrationValidity.cs' 'endDate.Date < today.Date' 'Expired memberships must be blocked.'
Assert-Contains $service 'RegistrationStatuses\.Active' 'Only active registrations may check in.'
Assert-Contains $service 'PaymentStatuses\.Succeeded' 'Only paid registrations may check in.'
Assert-Contains $service 'DuLieuKhuonMat[\s\S]*DangHoatDong' 'Face check-in requires active face data.'
Assert-Contains $service 'DongYSinhTracHoc[\s\S]*NgayRutLai' 'Face check-in requires active consent.'
Assert-Contains $service 'failedAttempts\s*>=\s*3' 'The third failed face attempt must escalate.'
Assert-Contains $service 'CheckInMethods\.Manual' 'Manual check-in must be distinguishable in history.'
Assert-Contains $service 'MaSuKien' 'Device entry events must support idempotency.'
Assert-Contains 'src/FitGym.Api/Features/Gate/GateEndpoints.cs' 'Gate:ApiKey' 'Gate API must require a configured device credential.'
if ((Get-Content -Raw $service) -match 'override|MoCua|CongG') { throw 'Check-in service must not invent gate-control or expiry override behavior.' }
if ((Get-Content -Raw $controller) -match 'Task<IActionResult>\s+Manual\(') { throw 'The staff Web must not offer manual check-in.' }
if (-not (Test-Path 'src/FitGym.Web/Views/CheckIns/Index.cshtml')) { throw 'Missing entry history view.' }
if (Test-Path 'src/FitGym.Web/Views/CheckIns/Manual.cshtml') { throw 'The manual check-in view is obsolete.' }

'check-in contract: PASS'
