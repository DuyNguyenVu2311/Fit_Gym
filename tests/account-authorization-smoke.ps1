param(
    [Parameter(Mandatory)][string]$ManagerUser,
    [Parameter(Mandatory)][string]$ManagerPassword,
    [Parameter(Mandatory)][string]$EmployeeUser,
    [Parameter(Mandatory)][string]$EmployeePassword,
    [string]$BaseUrl = 'https://localhost:7295'
)

$ErrorActionPreference = 'Stop'
$managerCookie = Join-Path $env:TEMP 'fitgym-manager-cookies.txt'
$employeeCookie = Join-Path $env:TEMP 'fitgym-employee-cookies.txt'
$headers = Join-Path $env:TEMP 'fitgym-authorization-headers.txt'

function Get-AntiForgeryToken([string]$Html) {
    $match = [regex]::Match($Html, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"')
    if (-not $match.Success) { throw 'Anti-forgery token not found.' }
    return $match.Groups[1].Value
}

function Login([string]$UserName, [string]$Password, [string]$Cookie) {
    $html = curl.exe -k -s -c $Cookie "$BaseUrl/Account/Login"
    $token = Get-AntiForgeryToken $html
    $status = curl.exe -k -s -o NUL -w '%{http_code}' -b $Cookie -c $Cookie `
        -X POST "$BaseUrl/Account/Login" `
        --data-urlencode "__RequestVerificationToken=$token" `
        --data-urlencode "UserNameOrEmail=$UserName" `
        --data-urlencode "Password=$Password"
    if ($status -ne '302') { throw "Login for $UserName returned HTTP $status." }
}

try {
    Login $ManagerUser $ManagerPassword $managerCookie
    $createHtml = curl.exe -k -s -b $managerCookie "$BaseUrl/Accounts/Create"
    $createToken = Get-AntiForgeryToken $createHtml
    $createStatus = curl.exe -k -s -o NUL -w '%{http_code}' -b $managerCookie `
        -X POST "$BaseUrl/Accounts/Create" `
        --data-urlencode "__RequestVerificationToken=$createToken" `
        --data-urlencode "UserName=$EmployeeUser" `
        --data-urlencode "$([char]69)mail=$EmployeeUser@example.test" `
        --data-urlencode "Password=$EmployeePassword" `
        --data-urlencode 'HoTen=Test Authorization Employee' `
        --data-urlencode 'ChucVu=Nhan vien kiem thu' `
        --data-urlencode 'NgayVaoLam=2026-11-19' `
        --data-urlencode 'Role=NhanVien'
    if ($createStatus -ne '302') { throw "Employee creation returned HTTP $createStatus." }

    Login $EmployeeUser $EmployeePassword $employeeCookie
    $employeeStatus = curl.exe -k -s -o NUL -D $headers -w '%{http_code}' -b $employeeCookie "$BaseUrl/Accounts"
    $headerText = Get-Content -Raw $headers
    if ($employeeStatus -ne '302' -or $headerText -notmatch '/Account/AccessDenied') {
        throw "Employee authorization expected redirect to AccessDenied, received HTTP $employeeStatus."
    }

    'account role authorization smoke: PASS'
}
finally {
    foreach ($path in @($managerCookie, $employeeCookie, $headers)) {
        if (Test-Path $path) { Remove-Item -LiteralPath $path -Force }
    }
}
