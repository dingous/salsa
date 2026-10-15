[CmdletBinding()]
param([string]$OutputPath, [switch]$SkipBuild, [switch]$SkipTracker, [switch]$StopRunning)
$ErrorActionPreference='Stop'
$avatarRoot=Split-Path $PSScriptRoot -Parent
if(!$OutputPath){$OutputPath=Join-Path $avatarRoot 'publish/Windows/AvatarStudio.exe'}
$OutputPath=[IO.Path]::GetFullPath($OutputPath)
if([IO.Path]::GetExtension($OutputPath) -ne '.exe'){throw 'OutputPath deve ser um executavel .exe.'}
$avatarRunning=@(Get-CimInstance Win32_Process | Where-Object {$_.ExecutablePath -and [string]::Equals($_.ExecutablePath,$OutputPath,[StringComparison]::OrdinalIgnoreCase)})
if($avatarRunning.Count -and !$StopRunning){throw "O aplicativo de destino está aberto. Feche-o pela bandeja ou use -StopRunning para atualizar: $OutputPath"}
foreach($avatarProcess in $avatarRunning){
    # Stop only the exact exported executable and its owned microphone/tray children.
    & taskkill.exe /PID $avatarProcess.ProcessId /T /F | Out-Null
    if($LASTEXITCODE -ne 0 -and (Get-Process -Id $avatarProcess.ProcessId -ErrorAction SilentlyContinue)){throw 'Não foi possível encerrar a instância exportada.'}
    $avatarWait=Get-Process -Id $avatarProcess.ProcessId -ErrorAction SilentlyContinue
    if($avatarWait -and !$avatarWait.WaitForExit(5000)){throw 'O aplicativo ainda está encerrando. Tente exportar novamente.'}
}
if(!$SkipBuild){& (Join-Path $PSScriptRoot 'build-corporate.ps1')}
$avatarSource=Join-Path $avatarRoot 'CorporateUnity/Build'
$avatarDestination=Split-Path $OutputPath -Parent
$avatarName=[IO.Path]::GetFileNameWithoutExtension($OutputPath)
New-Item -ItemType Directory -Force -Path $avatarDestination | Out-Null
$avatarCopyDeadline=[DateTime]::UtcNow.AddSeconds(10)
while($true){
    try{Copy-Item -LiteralPath (Join-Path $avatarSource 'CorporateAvatarStudio.exe') -Destination $OutputPath -Force;break}
    catch [System.IO.IOException]{
        # Windows may retain the image mapping briefly after process termination.
        if([DateTime]::UtcNow -ge $avatarCopyDeadline){throw}
        Start-Sleep -Milliseconds 250
    }
}
$avatarData=Join-Path $avatarDestination ($avatarName+'_Data')
New-Item -ItemType Directory -Force -Path $avatarData | Out-Null
Get-ChildItem -LiteralPath (Join-Path $avatarSource 'CorporateAvatarStudio_Data') -Force | Copy-Item -Destination $avatarData -Recurse -Force
foreach($avatarItem in Get-ChildItem -LiteralPath $avatarSource -Force){
    if($avatarItem.Name -in @('CorporateAvatarStudio.exe','CorporateAvatarStudio_Data')){continue}
    if($avatarItem.Name -notin @('D3D12','MonoBleedingEdge','UnityCrashHandler64.exe') -and $avatarItem.Extension -ne '.dll'){continue}
    Copy-Item -LiteralPath $avatarItem.FullName -Destination $avatarDestination -Recurse -Force
}
if(!$SkipTracker){Copy-Item -LiteralPath (Join-Path $avatarRoot 'Tracker') -Destination $avatarDestination -Recurse -Force}
foreach($avatarModel in @('CorporateHuman.vrm','CorporateWoman.vrm','Seed-san.vrm','AvaturnBase.glb')){
    $avatarBundled=Join-Path $avatarData "StreamingAssets/Avatars/$avatarModel"
    if(!(Test-Path -LiteralPath $avatarBundled)){throw "Modelo ausente: $avatarModel"}
    if((Get-FileHash -LiteralPath $avatarBundled).Hash -ne (Get-FileHash -LiteralPath (Join-Path $avatarRoot "CorporateUnity/Assets/StreamingAssets/Avatars/$avatarModel")).Hash){throw "Modelo desatualizado: $avatarModel"}
}
$avatarAssembly=Join-Path $avatarData 'Managed/WebcamAvatar.dll'
$avatarSalsa=Join-Path $avatarData 'Managed/SALSA-LipSync.dll'
if(!(Test-Path -LiteralPath $avatarSalsa)){throw 'Runtime SALSA ausente no pacote Windows.'}
if((Get-FileHash -LiteralPath $avatarSalsa).Hash -ne (Get-FileHash -LiteralPath (Join-Path $avatarSource 'CorporateAvatarStudio_Data/Managed/SALSA-LipSync.dll')).Hash){throw 'Runtime SALSA desatualizado no pacote.'}
if((Get-FileHash -LiteralPath $avatarAssembly).Hash -ne (Get-FileHash -LiteralPath (Join-Path $avatarSource 'CorporateAvatarStudio_Data/Managed/WebcamAvatar.dll')).Hash){throw 'Interface desatualizada no pacote.'}
Write-Output "Windows atualizado: $OutputPath (Cartoon + Corporativo)"
