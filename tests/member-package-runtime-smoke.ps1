param(
    [Parameter(Mandatory)][string]$ManagerUser,
    [Parameter(Mandatory)][string]$ManagerPassword,
    [string]$BaseUrl = 'https://localhost:7295'
)

$ErrorActionPreference = 'Stop'
$cookie = Join-Path $env:TEMP 'fitgym-member-package-cookies.txt'
$headers = Join-Path $env:TEMP 'fitgym-member-package-headers.txt'
$body = Join-Path $env:TEMP 'fitgym-member-package-body.html'

function Get-Token([string]$Html) {
    $match = [regex]::Match($Html, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"')
    if (-not $match.Success) { throw 'Anti-forgery token not found.' }
    return $match.Groups[1].Value
}

try {
    $login = curl.exe -k -s -c $cookie "$BaseUrl/Account/Login"
    $token = Get-Token $login
    $loginStatus = curl.exe -k -s -o NUL -w '%{http_code}' -b $cookie -c $cookie `
        -X POST "$BaseUrl/Account/Login" `
        --data-urlencode "__RequestVerificationToken=$token" `
        --data-urlencode "UserNameOrEmail=$ManagerUser" `
        --data-urlencode "Password=$ManagerPassword"
    if ($loginStatus -ne '302') { throw "Manager login returned HTTP $loginStatus." }

    foreach ($path in @('/Members', '/Packages')) {
        $status = curl.exe -k -s -o NUL -w '%{http_code}' -b $cookie "$BaseUrl$path"
        if ($status -ne '200') { throw "$path returned HTTP $status." }
    }

    $memberForm = curl.exe -k -s -b $cookie "$BaseUrl/Members/Create"
    $token = Get-Token $memberForm
    $memberStatus = curl.exe -k -s -o $body -D $headers -w '%{http_code}' -b $cookie `
        -X POST "$BaseUrl/Members/Create" `
        --data-urlencode "__RequestVerificationToken=$token" `
        --data-urlencode 'HoTen=__FITGYM_TEST_MEMBER__' `
        --data-urlencode 'NgaySinh=2000-01-01' `
        --data-urlencode 'SoDienThoai=0900000000' `
        --data-urlencode 'NgayThamGia=2026-11-19'
    if ($memberStatus -ne '302') { throw "Member creation returned HTTP $memberStatus." }
    $location = ([regex]::Match((Get-Content -Raw $headers), '(?im)^Location:\s*(.+?)\r?$')).Groups[1].Value.Trim()
    $memberId = ([regex]::Match($location, '(\d+)$')).Groups[1].Value
    if (-not $memberId) { throw 'Created member id was not returned.' }

    $duplicateForm = curl.exe -k -s -b $cookie "$BaseUrl/Members/Create"
    $token = Get-Token $duplicateForm
    $duplicateStatus = curl.exe -k -s -o $body -w '%{http_code}' -b $cookie `
        -X POST "$BaseUrl/Members/Create" `
        --data-urlencode "__RequestVerificationToken=$token" `
        --data-urlencode 'HoTen=__FITGYM_DUPLICATE_MEMBER__' `
        --data-urlencode 'SoDienThoai=0900000000' `
        --data-urlencode 'NgayThamGia=2026-11-19'
    if ($duplicateStatus -ne '200') { throw "Duplicate phone validation returned HTTP $duplicateStatus." }

    $details = curl.exe -k -s -b $cookie "$BaseUrl/Members/Details/$memberId"
    $token = Get-Token $details
    $statusChange = curl.exe -k -s -o NUL -w '%{http_code}' -b $cookie `
        -X POST "$BaseUrl/Members/SetStatus" `
        --data-urlencode "__RequestVerificationToken=$token" `
        --data-urlencode "id=$memberId" `
        --data-urlencode 'status=NgungHoatDong'
    if ($statusChange -ne '302') { throw "Member status change returned HTTP $statusChange." }

    $packageForm = curl.exe -k -s -b $cookie "$BaseUrl/Packages/Create"
    $token = Get-Token $packageForm
    $packageStatus = curl.exe -k -s -o $body -D $headers -w '%{http_code}' -b $cookie `
        -X POST "$BaseUrl/Packages/Create" `
        --data-urlencode "__RequestVerificationToken=$token" `
        --data-urlencode 'TenGoi=__FITGYM_TEST_PACKAGE__' `
        --data-urlencode 'ThoiHanThang=1' `
        --data-urlencode 'GiaTien=1000'
    if ($packageStatus -ne '302') { throw "Package creation returned HTTP $packageStatus." }

    $invalidForm = curl.exe -k -s -b $cookie "$BaseUrl/Packages/Create"
    $token = Get-Token $invalidForm
    $invalidStatus = curl.exe -k -s -o $body -w '%{http_code}' -b $cookie `
        -X POST "$BaseUrl/Packages/Create" `
        --data-urlencode "__RequestVerificationToken=$token" `
        --data-urlencode 'TenGoi=__FITGYM_INVALID_PACKAGE__' `
        --data-urlencode 'ThoiHanThang=0' `
        --data-urlencode 'GiaTien=-1'
    if ($invalidStatus -ne '200') { throw "Invalid package validation returned HTTP $invalidStatus." }

    'member/package runtime smoke: PASS'
}
finally {
    foreach ($path in @($cookie, $headers, $body)) {
        if (Test-Path $path) { Remove-Item -LiteralPath $path -Force }
    }
}
