$ErrorActionPreference='Stop'
[Console]::OutputEncoding=New-Object System.Text.UTF8Encoding($false)
. (Join-Path $PSScriptRoot 'windows_voice_catalog.ps1')
$ttsCatalog=@(Get-DingousVoices)
@{voices=@($ttsCatalog | ForEach-Object {$_.Name})} | ConvertTo-Json -Compress
