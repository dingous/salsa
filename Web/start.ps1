[CmdletBinding()]
param([switch]$NoBrowser,[switch]$Restart)
$ErrorActionPreference = 'Stop'
$studioRoot = Split-Path -Parent $PSScriptRoot
$studioPython = Join-Path $studioRoot 'Tracker\.venv\Scripts\python.exe'
$studioBasePython = (& $studioPython -c 'import sys; print(sys._base_executable)').Trim()
$studioUrl = 'http://127.0.0.1:8765'
$studioServer = Join-Path $PSScriptRoot 'server.py'
$studioRevision = (Get-FileHash -LiteralPath $studioServer -Algorithm SHA256).Hash.ToLowerInvariant()
function Test-Studio {
    try { $studioSession = Invoke-RestMethod "$studioUrl/api/session" -TimeoutSec 1; return $studioSession.app -eq 'AvatarStudioWeb' -and $studioSession.version -eq 2 -and $studioSession.revision -eq $studioRevision } catch { return $false }
}
if ($Restart -or -not (Test-Studio)) {
    $studioListener = Get-NetTCPConnection -LocalAddress 127.0.0.1 -LocalPort 8765 -State Listen -ErrorAction SilentlyContinue
    if ($studioListener) {
        $studioProcess = Get-CimInstance Win32_Process -Filter "ProcessId=$($studioListener.OwningProcess)"
        $studioScriptMatches = $studioProcess.CommandLine.Replace('/','\').IndexOf($studioServer,[StringComparison]::OrdinalIgnoreCase) -ge 0
        if ($studioProcess.ExecutablePath -notin @($studioPython,$studioBasePython) -or -not $studioScriptMatches) { throw 'A porta 8765 está ocupada por outro aplicativo. A ponte não foi substituída.' }
        $studioOldSession = Invoke-RestMethod "$studioUrl/api/session" -TimeoutSec 2
        if ($studioOldSession.app -ne 'AvatarStudioWeb') { throw 'O serviço nesta porta não é o Avatar Studio.' }
        $studioHeaders = @{'X-Avatar-Token'=$studioOldSession.token; 'X-Avatar-Client'='launcher-update'}
        $studioOldState = Invoke-RestMethod "$studioUrl/api/status" -Headers $studioHeaders -TimeoutSec 2
        foreach ($studioDevice in @('tracking','camera')) {
            $studioOwner = if ($studioDevice -eq 'tracking') { $studioOldState.trackerOwner } else { $studioOldState.cameraOwner }
            $studioHeaders['X-Avatar-Client'] = if ($studioOwner) { $studioOwner } else { 'launcher-update' }
            Invoke-RestMethod "$studioUrl/api/$studioDevice/stop" -Method Post -Headers $studioHeaders -ContentType 'application/json' -Body '{}' -TimeoutSec 5 | Out-Null
        }
        Stop-Process -Id $studioListener.OwningProcess
        for ($studioExitAttempt=0; $studioExitAttempt -lt 20; $studioExitAttempt++) {
            if (-not (Get-NetTCPConnection -LocalAddress 127.0.0.1 -LocalPort 8765 -State Listen -ErrorAction SilentlyContinue)) { break }
            Start-Sleep -Milliseconds 100
        }
    }
    Start-Process -FilePath $studioPython -WindowStyle Hidden -ArgumentList @('"' + $studioServer + '"') -WorkingDirectory $studioRoot -RedirectStandardOutput (Join-Path $studioRoot 'web-server.log') -RedirectStandardError (Join-Path $studioRoot 'web-server-errors.log') | Out-Null
    $studioReady = $false
    for ($studioAttempt = 0; $studioAttempt -lt 30; $studioAttempt++) {
        if (Test-Studio) { $studioReady = $true; break }
        Start-Sleep -Milliseconds 200
    }
    if (-not $studioReady) { throw 'A ponte local não iniciou. Confira web-server-errors.log.' }
}
if (-not $NoBrowser) { Start-Process $studioUrl }
