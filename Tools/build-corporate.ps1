param([switch]$Development)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
& (Join-Path $taskRoot 'Tracker/.venv/Scripts/python.exe') (Join-Path $PSScriptRoot 'install_salsa.py')
if($LASTEXITCODE -ne 0){throw 'SALSA runtime não instalado.'}
$taskUnity = 'C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe'
if (-not (Test-Path -LiteralPath $taskUnity)) { throw 'Instale Unity 6000.4.5f1 com Windows Build Support.' }
$taskArguments=@('-batchmode','-quit','-projectPath',('"'+(Join-Path $taskRoot 'CorporateUnity')+'"'),'-executeMethod','WebcamAvatar.Editor.CorporateBuild.Windows','-logFile',('"'+(Join-Path $taskRoot 'corporate-build.log')+'"'))
if($Development){$taskArguments+='--development-build'}
$taskProcess = Start-Process -FilePath $taskUnity -ArgumentList $taskArguments -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if ($taskProcess.ExitCode -ne 0) { throw 'Build falhou. Consulte corporate-build.log.' }
Write-Host 'Pronto: Iniciar Corporate Avatar Studio.cmd'
