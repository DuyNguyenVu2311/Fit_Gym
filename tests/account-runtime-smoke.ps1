param(
    [Parameter(Mandatory)][string]$UserName,
    [Parameter(Mandatory)][string]$Password,
    [string]$BaseUrl = 'https://localhost:7295'
)

$ErrorActionPreference = 'Stop'
$cookie = Join-Path $env:TEMP 'fitgym-account-runtime-cookies.txt'
try {
    $loginHtml = curl.exe -k -s -c $cookie "$BaseUrl/Account/Login"
    $tokenMatch = [regex]::Match($loginHtml, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"')
    if (-not $tokenMatch.Success) { throw 'Anti-forgery token not found.' }

    $token = $tokenMatch.Groups[1].Value
    $postStatus = curl.exe -k -s -o NUL -w '%{http_code}' -b $cookie -c $cookie `
        -X POST "$BaseUrl/Account/Login" `
        --data-urlencode "__RequestVerificationToken=$token" `
        --data-urlencode "UserNameOrEmail=$UserName" `
        --data-urlencode "Password=$Password" `
        --data-urlencode 'RememberMe=false'
    $accountsStatus = curl.exe -k -s -o NUL -w '%{http_code}' -b $cookie "$BaseUrl/Accounts"

    if ($postStatus -ne '302') { throw "Expected login HTTP 302, received $postStatus." }
    if ($accountsStatus -ne '200') { throw "Expected manager Accounts HTTP 200, received $accountsStatus." }
    'account runtime smoke: PASS'
}
finally {
    if (Test-Path $cookie) { Remove-Item -LiteralPath $cookie -Force }
}
