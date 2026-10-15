param([Parameter(Mandatory=$true)][string]$TextPath,
 [string]$VoiceName='Microsoft Daniel',
 [ValidateRange(-3,3)][int]$Rate=-1,
 [ValidateRange(0,100)][int]$Volume=85,
 [string]$OutputPath)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'windows_voice_catalog.ps1')
$dingousCatalog=@(Get-DingousVoices)
$dingousSelected=$dingousCatalog | Where-Object {$_.Name -eq $VoiceName} | Select-Object -First 1
if(!$dingousSelected){$dingousSelected=$dingousCatalog | Where-Object {$_.Gender -eq 'Male' -and ($_.Language -split ';') -contains '416'} | Select-Object -First 1}
if(!$dingousSelected){$dingousSelected=$dingousCatalog | Where-Object {($_.Language -split ';') -contains '416'} | Select-Object -First 1}
if(!$dingousSelected){throw 'Instale uma voz local em português do Brasil nas configurações de fala do Windows.'}
$dingousVoice=New-Object -ComObject SAPI.SpVoice
$dingousWave=$null
try {
 $dingousVoice.Voice=$dingousSelected.Token
 $dingousVoice.Rate=$Rate
 $dingousVoice.Volume=$Volume
 if($OutputPath){
  $dingousWave=New-Object -ComObject SAPI.SpFileStream
  $dingousWave.Format.Type=22
  $dingousWave.Open([IO.Path]::GetFullPath($OutputPath),3,$false)
  $dingousVoice.AudioOutputStream=$dingousWave
 }
 $dingousText=[IO.File]::ReadAllText($TextPath,[Text.Encoding]::UTF8)
 # SVSFIsNotXML: model output is plain text, never executable SSML.
 [void]$dingousVoice.Speak($dingousText,16)
} finally {
 if($dingousWave){$dingousWave.Close();[void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($dingousWave)}
 [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($dingousVoice)
}