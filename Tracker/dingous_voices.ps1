$ErrorActionPreference='Stop'
[Console]::OutputEncoding=New-Object System.Text.UTF8Encoding($false)
Add-Type -AssemblyName System.Speech
$ttsEnumerator=New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
 $ttsNames=@($ttsEnumerator.GetInstalledVoices() | Where-Object {$_.Enabled} | ForEach-Object {$_.VoiceInfo.Name})
 @{voices=$ttsNames} | ConvertTo-Json -Compress
} finally {$ttsEnumerator.Dispose()}
