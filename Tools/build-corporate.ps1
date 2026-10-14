$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskUnity = 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe'
if (-not (Test-Path -LiteralPath $taskUnity)) { throw 'Instale Unity 6000.4.5f1 com Windows Build Support.' }
$taskProcess = Start-Process -FilePath $taskUnity -ArgumentList @('-batchmode','-quit','-projectPath',('"'+(Join-Path $taskRoot 'CorporateUnity')+'"'),'-executeMethod','WebcamAvatar.Editor.CorporateBuild.Windows','-logFile',('"'+(Join-Path $taskRoot 'corporate-build.log')+'"')) -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if ($taskProcess.ExitCode -ne 0) { throw 'Build falhou. Consulte corporate-build.log.' }
Write-Host 'Pronto: Iniciar Corporate Avatar Studio.cmd'
