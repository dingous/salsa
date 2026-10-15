$ErrorActionPreference = 'Stop'
$dingousSdk = 'C:/Program Files (x86)/Android/android-sdk'
$dingousAdb = Join-Path $dingousSdk 'platform-tools/adb.exe'
if(@(& $dingousAdb devices 2>$null) -match '^emulator-5556\s+device') { Write-Output 'DingousAvatarE2E já está conectado na porta 5556; configuração preservada.'; return }
$dingousEmulator = Join-Path $dingousSdk 'emulator/emulator.exe'
$dingousAvd = Join-Path $env:USERPROFILE '.android/avd/DingousAvatarE2E.avd/config.ini'
if (!(Test-Path -LiteralPath $dingousAvd)) { throw 'Crie o AVD de teste DingousAvatarE2E/API 35 antes de iniciar.' }
$dingousConfig = Get-Content -LiteralPath $dingousAvd -Raw
foreach ($dingousSetting in @{'hw.keyboard'='yes'; 'hw.lcd.density'='160'; 'disk.dataPartition.size'='16G'}.GetEnumerator()) {
    $dingousPattern = '(?m)^' + [regex]::Escape($dingousSetting.Key) + '=.*$'
    $dingousLine = $dingousSetting.Key + '=' + $dingousSetting.Value
    if ($dingousConfig -match $dingousPattern) { $dingousConfig = $dingousConfig -replace $dingousPattern,$dingousLine }
    else { $dingousConfig += "`n$dingousLine`n" }
}
[IO.File]::WriteAllText($dingousAvd,$dingousConfig,[Text.ASCIIEncoding]::new())
$dingousRoot = Split-Path $PSScriptRoot -Parent
$dingousLogs = Join-Path $dingousRoot 'Validation'
New-Item -ItemType Directory -Force $dingousLogs | Out-Null
Start-Process -FilePath $dingousEmulator -ArgumentList @('-avd','DingousAvatarE2E','-port','5556','-memory','4096','-no-snapshot','-no-boot-anim','-skin','960x540','-camera-front','webcam0','-camera-back','none','-gpu','swiftshader_indirect') -WindowStyle Normal -RedirectStandardOutput (Join-Path $dingousLogs 'fitness-emulator.log') -RedirectStandardError (Join-Path $dingousLogs 'fitness-emulator-error.log')
Write-Output 'Emulador assistido aberto: DingousAvatarE2E, porta 5556, 960x540, teclado físico habilitado.'
Write-Output 'Após o boot: adb -s emulator-5556 reverse tcp:8767 tcp:8767. Permissões e pareamento usam a interface do Android.'
