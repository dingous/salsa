param([Parameter(Mandatory=$true)][string]$TextPath,
 [string]$VoiceName='Microsoft Daniel',
 [ValidateRange(-3,3)][int]$Rate=-1,
 [string]$OutputPath)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Speech
$dingousVoice=New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
 $dingousPortuguese=@($dingousVoice.GetInstalledVoices() | Where-Object {$_.Enabled -and $_.VoiceInfo.Culture.Name -eq 'pt-BR'})
 $dingousSelected=$dingousPortuguese | Where-Object {$_.VoiceInfo.Name -eq $VoiceName} | Select-Object -First 1
 if(!$dingousSelected){$dingousSelected=$dingousPortuguese | Select-Object -First 1}
 if(!$dingousSelected){throw 'Instale uma voz local em português do Brasil nas configurações de fala do Windows.'}
 $dingousVoice.SelectVoice($dingousSelected.VoiceInfo.Name)
 $dingousVoice.Rate=$Rate
 $dingousVoice.Volume=85
 if($OutputPath){$dingousVoice.SetOutputToWaveFile([IO.Path]::GetFullPath($OutputPath))}
 $dingousPrompt=New-Object System.Speech.Synthesis.PromptBuilder($dingousSelected.VoiceInfo.Culture)
 $dingousText=[IO.File]::ReadAllText($TextPath,[Text.Encoding]::UTF8)
 # AppendText escapes text: model output never becomes executable SSML.
 foreach($dingousParagraph in ($dingousText -split '\r?\n\s*\r?\n')){
  if(!$dingousParagraph.Trim()){continue}
  $dingousPrompt.AppendText($dingousParagraph.Trim())
  $dingousPrompt.AppendBreak([TimeSpan]::FromMilliseconds(280))
 }
 $dingousVoice.Speak($dingousPrompt)
} finally {$dingousVoice.Dispose()}
