$ErrorActionPreference = 'Stop'

function Assert-Contains([string]$Path, [string]$Pattern, [string]$Message) {
    if (-not (Test-Path $Path)) { throw "Missing file: $Path" }
    if ((Get-Content -Raw $Path) -notmatch $Pattern) { throw $Message }
}

$program = 'src/FitGym.Api/Program.cs'
$endpoints = 'src/FitGym.Api/Features/Member/MemberEndpoints.cs'
$dtos = 'src/FitGym.Api/Features/Member/MemberDtos.cs'
$client = 'mobile/fit_gym_mobile/lib/core/network/api_client.dart'
$login = 'mobile/fit_gym_mobile/lib/features/auth/login_page.dart'
$homeScreen = 'mobile/fit_gym_mobile/lib/features/home/member_home_page.dart'

Assert-Contains $program 'AddBearerToken' 'API must issue bearer tokens through ASP.NET Core Identity.'
Assert-Contains $program 'UseAuthentication\(\)' 'API authentication middleware is missing.'
Assert-Contains $program 'MapMemberEndpoints' 'Member endpoints must be mapped.'
Assert-Contains $endpoints 'RequireAuthorization' 'Member endpoints must require authentication.'
Assert-Contains $endpoints 'MaTaiKhoan' 'Authenticated Identity user must resolve to a member.'
Assert-Contains $endpoints '/api/member/me' 'Member profile endpoint is missing.'
Assert-Contains $endpoints '/api/member/subscriptions/current' 'Current subscription endpoint is missing.'
Assert-Contains $endpoints '/api/member/payments' 'Payment status endpoint is missing.'
Assert-Contains $endpoints '/api/member/check-ins' 'Check-in history endpoint is missing.'
if ((Get-Content -Raw $dtos) -match 'VectorKhuonMat|byte\[\]') { throw 'API DTOs must never expose biometric vectors.' }

Assert-Contains $client 'FITGYM_API_URL' 'Flutter API base URL must be configured by dart-define.'
Assert-Contains $client 'Authorization' 'Flutter API client must send the bearer token.'
Assert-Contains $login 'Đăng nhập' 'Flutter login screen is missing.'
Assert-Contains $homeScreen 'CircularProgressIndicator' 'Flutter screens must provide loading state.'
Assert-Contains $homeScreen 'error' 'Flutter screens must provide error state.'
Assert-Contains $homeScreen 'isEmpty' 'Flutter screens must provide empty state.'

'mobile/API contract: PASS'
