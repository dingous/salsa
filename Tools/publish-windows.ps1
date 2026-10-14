[CmdletBinding()]
param([string]$OutputPath, [switch]$SkipBuild, [switch]$SkipTracker)
$ErrorActionPreference='Stop'
$avatarRoot=Split-Path $PSScriptRoot -Parent
if(!$OutputPath){$OutputPath=Join-Path $avatarRoot 'publish/Windows/AvatarStudio.exe'}
$OutputPath=[IO.Path]::GetFullPath($OutputPath)
if([IO.Path]::GetExtension($OutputPath) -ne '.exe'){throw 'OutputPath deve ser um executavel .exe.'}
if(!$SkipBuild){& (Join-Path $PSScriptRoot 'build-corporate.ps1')}
$avatarSource=Join-Path $avatarRoot 'CorporateUnity/Build'
$avatarDestination=Split-Path $OutputPath -Parent
$avatarName=[IO.Path]::GetFileNameWithoutExtension($OutputPath)
New-Item -ItemType Directory -Force -Path $avatarDestination | Out-Null
Copy-Item -LiteralPath (Join-Path $avatarSource 'CorporateAvatarStudio.exe') -Destination $OutputPath -Force
$avatarData=Join-Path $avatarDestination ($avatarName+'_Data')
New-Item -ItemType Directory -Force -Path $avatarData | Out-Null
Get-ChildItem -LiteralPath (Join-Path $avatarSource 'CorporateAvatarStudio_Data') -Force | Copy-Item -Destination $avatarData -Recurse -Force
foreach($avatarItem in Get-ChildItem -LiteralPath $avatarSource -Force){
    if($avatarItem.Name -in @('CorporateAvatarStudio.exe','CorporateAvatarStudio_Data')){continue}
    Copy-Item -LiteralPath $avatarItem.FullName -Destination $avatarDestination -Recurse -Force
}
if(!$SkipTracker){Copy-Item -LiteralPath (Join-Path $avatarRoot 'Tracker') -Destination $avatarDestination -Recurse -Force}
foreach($avatarModel in @('CorporateHuman.vrm','Seed-san.vrm')){
    $avatarBundled=Join-Path $avatarData "StreamingAssets/Avatars/$avatarModel"
    if(!(Test-Path -LiteralPath $avatarBundled)){throw "Modelo ausente: $avatarModel"}
    if((Get-FileHash -LiteralPath $avatarBundled).Hash -ne (Get-FileHash -LiteralPath (Join-Path $avatarRoot "CorporateUnity/Assets/StreamingAssets/Avatars/$avatarModel")).Hash){throw "Modelo desatualizado: $avatarModel"}
}
$avatarAssembly=Join-Path $avatarData 'Managed/WebcamAvatar.dll'
if((Get-FileHash -LiteralPath $avatarAssembly).Hash -ne (Get-FileHash -LiteralPath (Join-Path $avatarSource 'CorporateAvatarStudio_Data/Managed/WebcamAvatar.dll')).Hash){throw 'Interface desatualizada no pacote.'}
Write-Output "Windows atualizado: $OutputPath (Cartoon + Corporativo)"
