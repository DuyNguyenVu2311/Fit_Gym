param(
    [Parameter(Mandatory)][string]$BootstrapUser,
    [Parameter(Mandatory)][string]$BootstrapPassword,
    [Parameter(Mandatory)][string]$OperatorUser,
    [Parameter(Mandatory)][string]$OperatorPassword,
    [string]$BaseUrl = 'https://localhost:7295'
)

$ErrorActionPreference = 'Stop'
$bootstrapCookie = Join-Path $env:TEMP 'fitgym-registration-bootstrap.txt'
$operatorCookie = Join-Path $env:TEMP 'fitgym-registration-operator.txt'
$headers = Join-Path $env:TEMP 'fitgym-registration-headers.txt'
$body = Join-Path $env:TEMP 'fitgym-registration-body.html'

function Get-Token([string]$Html) {
    $match = [regex]::Match($Html, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"')
    if (-not $match.Success) { throw 'Anti-forgery token not found.' }
    return $match.Groups[1].Value
}

function Login([string]$UserName, [string]$Password, [string]$Cookie) {
    $html = curl.exe -k -s -c $Cookie "$BaseUrl/Account/Login"
    $token = Get-Token $html
    $status = curl.exe -k -s -o NUL -w '%{http_code}' -b $Cookie -c $Cookie `
        -X POST "$BaseUrl/Account/Login" `
        --data-urlencode "__RequestVerificationToken=$token" `
        --data-urlencode "UserNameOrEmail=$UserName" `
        --data-urlencode "Password=$Password"
    if ($status -ne '302') { throw "Login for $UserName returned HTTP $status." }
}

function Read-RedirectId {
    $headerText = Get-Content -Raw $headers
    $location = ([regex]::Match($headerText, '(?im)^Location:\s*(.+?)\r?$')).Groups[1].Value.Trim()
    $id = ([regex]::Match($location, '(\d+)$')).Groups[1].Value
    if (-not $id) { throw "Redirect id not found in $location." }
    return $id
}

try {
    Login $BootstrapUser $BootstrapPassword $bootstrapCookie
    $accountForm = curl.exe -k -s -b $bootstrapCookie "$BaseUrl/Accounts/Create"
    $token = Get-Token $accountForm
    $accountStatus = curl.exe -k -s -o $body -D $headers -w '%{http_code}' -b $bootstrapCookie `
        -X POST "$BaseUrl/Accounts/Create" `
        --data-urlencode "__RequestVerificationToken=$token" `
        --data-urlencode "UserName=$OperatorUser" `
        --data-urlencode "Email=$OperatorUser@example.test" `
        --data-urlencode "Password=$OperatorPassword" `
        --data-urlencode 'HoTen=__FITGYM_TEST_OPERATOR__' `
        --data-urlencode 'ChucVu=Quan ly kiem thu' `
        --data-urlencode "NgayVaoLam=$([DateTime]::Today.ToString('yyyy-MM-dd'))" `
        --data-urlencode 'Role=QuanLy'
    if ($accountStatus -ne '302') { throw "Operator creation returned HTTP $accountStatus." }

    Login $OperatorUser $OperatorPassword $operatorCookie

    $memberForm = curl.exe -k -s -b $operatorCookie "$BaseUrl/Members/Create"
    $token = Get-Token $memberForm
    $memberStatus = curl.exe -k -s -o $body -D $headers -w '%{http_code}' -b $operatorCookie `
        -X POST "$BaseUrl/Members/Create" `
        --data-urlencode "__RequestVerificationToken=$token" `
        --data-urlencode 'HoTen=__FITGYM_REGISTRATION_MEMBER__' `
        --data-urlencode 'SoDienThoai=0900000001' `
        --data-urlencode "NgayThamGia=$([DateTime]::Today.ToString('yyyy-MM-dd'))"
    if ($memberStatus -ne '302') { throw "Member creation returned HTTP $memberStatus." }
    $memberId = Read-RedirectId

    $packageForm = curl.exe -k -s -b $operatorCookie "$BaseUrl/Packages/Create"
    $token = Get-Token $packageForm
    $packageStatus = curl.exe -k -s -o $body -D $headers -w '%{http_code}' -b $operatorCookie `
        -X POST "$BaseUrl/Packages/Create" `
        --data-urlencode "__RequestVerificationToken=$token" `
        --data-urlencode 'TenGoi=__FITGYM_REGISTRATION_PACKAGE__' `
        --data-urlencode 'ThoiHanThang=2' `
        --data-urlencode 'GiaTien=250000'
    if ($packageStatus -ne '302') { throw "Package creation returned HTTP $packageStatus." }
    $packageId = Read-RedirectId

    $registrationForm = curl.exe -k -s -b $operatorCookie "$BaseUrl/Registrations/Create"
    $token = Get-Token $registrationForm
    $registrationQuote = (curl.exe -k -s -b $operatorCookie "$BaseUrl/Registrations/Quote?memberId=$memberId&packageId=$packageId&startDate=$([DateTime]::Today.ToString('yyyy-MM-dd'))") | ConvertFrom-Json
    $registrationStatus = curl.exe -k -s -o $body -D $headers -w '%{http_code}' -b $operatorCookie `
        -X POST "$BaseUrl/Registrations/Create" `
        --data-urlencode "__RequestVerificationToken=$token" `
        --data-urlencode "MaHoiVien=$memberId" `
        --data-urlencode "MaGoiTap=$packageId" `
        --data-urlencode "QuoteToken=$($registrationQuote.quoteToken)" `
        --data-urlencode "NgayBatDau=$([DateTime]::Today.ToString('yyyy-MM-dd'))" `
        --data-urlencode 'PhuongThucThanhToan=TienMat' `
        --data-urlencode 'CashReceived=true' `
        --data-urlencode 'DongYSinhTracHoc=true'
    if ($registrationStatus -ne '302') { throw "Cash registration returned HTTP $registrationStatus." }
    $registrationId = Read-RedirectId

    $renewForm = curl.exe -k -s -b $operatorCookie "$BaseUrl/Registrations/Renew/$registrationId"
    $token = Get-Token $renewForm
    $renewalQuote = (curl.exe -k -s -b $operatorCookie "$BaseUrl/Registrations/RenewalQuote?registrationId=$registrationId&packageId=$packageId") | ConvertFrom-Json
    $renewStatus = curl.exe -k -s -o $body -D $headers -w '%{http_code}' -b $operatorCookie `
        -X POST "$BaseUrl/Registrations/Renew" `
        --data-urlencode "__RequestVerificationToken=$token" `
        --data-urlencode "MaDangKyGoc=$registrationId" `
        --data-urlencode 'HoTenHoiVien=__FITGYM_REGISTRATION_MEMBER__' `
        --data-urlencode "MaGoiTap=$packageId" `
        --data-urlencode "QuoteToken=$($renewalQuote.quoteToken)" `
        --data-urlencode 'PhuongThucThanhToan=QR'
    if ($renewStatus -ne '302') { throw "QR renewal returned HTTP $renewStatus." }

    'registration/payment runtime smoke: PASS'
}
finally {
    foreach ($path in @($bootstrapCookie, $operatorCookie, $headers, $body)) {
        if (Test-Path $path) { Remove-Item -LiteralPath $path -Force }
    }
}
