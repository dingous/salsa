$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskChatTrade = 'D:\Git\Diego\ChatTrade\ChatTrande'
$taskAdminDll = Join-Path $taskChatTrade 'artifacts/avatar-local-admin-secure/ChatTrade.Admin.dll'
if (-not (Test-Path -LiteralPath $taskAdminDll)) { throw 'Compile o admin ChatTrade antes de conectar.' }
if (-not (Get-NetTCPConnection -LocalPort 6300 -State Listen -ErrorAction SilentlyContinue)) {
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:Api__BaseUrl = 'http://127.0.0.1:5100'
    $env:Api__InternalUrl = 'http://127.0.0.1:5100'
    $env:Api__BrowserBaseUrl = 'http://127.0.0.1:5100'
    Start-Process dotnet -ArgumentList @($taskAdminDll,'--urls','http://127.0.0.1:6300') -WorkingDirectory (Join-Path $taskChatTrade 'src/ChatTrade.Admin') -WindowStyle Hidden
    $taskDeadline = (Get-Date).AddSeconds(30)
    while (-not (Get-NetTCPConnection -LocalPort 6300 -State Listen -ErrorAction SilentlyContinue)) {
        if ((Get-Date) -gt $taskDeadline) { throw 'O admin não iniciou na porta 6300.' }
        Start-Sleep -Milliseconds 500
    }
}
& (Join-Path $taskRoot 'Tracker/.venv/Scripts/python.exe') (Join-Path $PSScriptRoot 'connect-chattrade-avatar.py') --open
if ($LASTEXITCODE -ne 0) { throw 'Conexão não concluída.' }
Write-Host 'Reabra o Avatar para usar a sessão autenticada.'

