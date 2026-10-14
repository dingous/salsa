param([Parameter(Mandatory=$true)][string]$TextPath)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Speech
$dingousVoice=New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
 $dingousPortuguese=$dingousVoice.GetInstalledVoices() | Where-Object {$_.Enabled -and $_.VoiceInfo.Culture.Name -like 'pt-*'} | Select-Object -First 1
 if($dingousPortuguese){$dingousVoice.SelectVoice($dingousPortuguese.VoiceInfo.Name)}
 $dingousVoice.Speak([IO.File]::ReadAllText($TextPath,[Text.Encoding]::UTF8))
} finally {$dingousVoice.Dispose()}
