$ErrorActionPreference='Stop'
$dingousRoot=Split-Path $PSScriptRoot -Parent
$dingousUnity='C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe'
$dingousLog=Join-Path $dingousRoot 'Validation/android-build.log'
New-Item -ItemType Directory -Force (Split-Path $dingousLog) | Out-Null
$dingousBuild=Start-Process -FilePath $dingousUnity -ArgumentList @('-batchmode','-quit','-buildTarget','Android','-projectPath',('"'+(Join-Path $dingousRoot 'CorporateUnity')+'"'),'-executeMethod','WebcamAvatar.Editor.CorporateBuild.Android','-logFile',('"'+$dingousLog+'"')) -WindowStyle Hidden -PassThru
$dingousBuild.WaitForExit()
if($dingousBuild.ExitCode -ne 0){throw "Build Android falhou: $dingousLog"}
$dingousOutput=Join-Path $dingousRoot 'publish/Android'
New-Item -ItemType Directory -Force $dingousOutput | Out-Null
Copy-Item -LiteralPath (Join-Path $dingousRoot 'CorporateUnity/Build/Android/DingousAvatar.apk') -Destination $dingousOutput -Force
Write-Output "APK gerado: $dingousOutput/DingousAvatar.apk"
