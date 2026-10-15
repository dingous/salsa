[CmdletBinding()]
param(
    [string]$Executable,
    [string[]]$Cases,
    [switch]$Graphical,
    [string]$SpeechFile,
    [ValidateRange(30,1800)][int]$TimeoutSeconds=240
)
$ErrorActionPreference='Stop'
$dingousRoot=Split-Path $PSScriptRoot -Parent
if(!$Executable){$Executable=Join-Path $dingousRoot 'publish/Windows/AvatarStudio.exe'}
$Executable=[IO.Path]::GetFullPath($Executable)
if(!(Test-Path -LiteralPath $Executable)){throw "Executável ausente: $Executable"}
$dingousAssembly=Join-Path (Split-Path $Executable -Parent) ([IO.Path]::GetFileNameWithoutExtension($Executable)+'_Data/Managed/WebcamAvatar.dll')
$dingousAssemblyHash=(Get-FileHash -LiteralPath $dingousAssembly -Algorithm SHA256).Hash
$dingousFolder=Join-Path $dingousRoot ('Validation/release-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $dingousFolder -Force | Out-Null
$dingousResults=@()
$dingousCases=[ordered]@{
    'presets-only'='CORPORATE_PRESETS_RESULT=True'
    'female-rig'='CORPORATE_FEMALE_RIG_RESULT=True'
    'rig-parity'='CORPORATE_RIG_PARITY_RESULT=True'
    'contacts-only'='CORPORATE_CONTACT_RESULT=True'
    'agent-isolation'='CORPORATE_AGENT_ISOLATION_RESULT=True'
    'avaturn'='CORPORATE_AVATURN_RESULT=True'
    'metahuman-fixture'='CORPORATE_METAHUMAN_PREPARED_RESULT=True'
    'face-only'='CORPORATE_FACE_RESULT=True'
    'partner-only'='CORPORATE_PARTNER_RESULT=True'
    'lab-only'='CORPORATE_LAB_RESULT=True'
    'lipsync-only'='CORPORATE_LIPSYNC_RESULT=True'
    'decorations-only'='CORPORATE_DECORATIONS_RESULT=True'
    'startup-only'='CORPORATE_STARTUP_RESULT=True'
    'full-studio'='CORPORATE_RESULT=True'
}
if(!$Cases){$Cases=@('presets-only','female-rig','rig-parity','contacts-only','agent-isolation','avaturn','metahuman-fixture')}
foreach($dingousName in $Cases){if(!$dingousCases.Contains($dingousName)){throw "Caso desconhecido: $dingousName"}}
if(!$Graphical -and @($Cases | Where-Object {$_ -in @('lab-only','lipsync-only','decorations-only','full-studio')}).Count){throw 'Esses casos usam áudio ou captura renderizada. Execute com -Graphical.'}
if($Cases -contains 'lipsync-only' -and (!$SpeechFile -or !(Test-Path -LiteralPath $SpeechFile))){throw 'Informe SpeechFile WAV para o teste SALSA.'}
foreach($dingousCase in $dingousCases.GetEnumerator()){
    if($dingousCase.Key -notin $Cases){continue}
    $dingousLog=Join-Path $dingousFolder ($dingousCase.Key+'.log')
    $dingousStarted=Get-Date
    $dingousArguments=@('-batchmode','--corporate-smoke','-logFile',('"'+$dingousLog+'"'))
    if(!$Graphical){$dingousArguments+='-nographics'}
    if($dingousCase.Key -eq 'avaturn'){
        $dingousArguments+=@('--avaturn-file',('"'+(Join-Path $dingousRoot 'Web/assets/AvaturnBase.glb')+'"'))
    }elseif($dingousCase.Key -eq 'metahuman-fixture'){
        # Existing MakeHuman exercises the prepared-import path; no MetaHuman art asset is bundled.
        $dingousArguments+=@('--metahuman-file',('"'+(Join-Path $dingousRoot 'Web/assets/CorporateHuman.vrm')+'"'))
    }else{$dingousArguments+='--'+$dingousCase.Key}
    if($dingousCase.Key -eq 'lipsync-only'){$dingousArguments+=@('--speech-file',('"'+[IO.Path]::GetFullPath($SpeechFile)+'"'))}
    $dingousProcess=Start-Process -FilePath $Executable -WorkingDirectory $dingousRoot -ArgumentList $dingousArguments -WindowStyle Hidden -PassThru
    $dingousTimedOut=!$dingousProcess.WaitForExit($TimeoutSeconds*1000)
    if($dingousTimedOut){$dingousProcess.Kill();$dingousProcess.WaitForExit()}
    $dingousText=if(Test-Path -LiteralPath $dingousLog){Get-Content -LiteralPath $dingousLog -Raw}else{''}
    $dingousPassed=!$dingousTimedOut -and $dingousProcess.ExitCode -eq 0 -and $dingousText.Contains($dingousCase.Value) -and !$dingousText.Contains('CORPORATE_FAIL:')
    $dingousResults+=[pscustomobject]@{test=$dingousCase.Key;passed=$dingousPassed;exitCode=$dingousProcess.ExitCode;timedOut=$dingousTimedOut;seconds=[Math]::Round(((Get-Date)-$dingousStarted).TotalSeconds,1);log=$dingousLog;graphical=[bool]$Graphical;executable=$Executable;assemblySha256=$dingousAssemblyHash}
    $dingousResults | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $dingousFolder 'results.json') -Encoding UTF8
    Write-Output "$($dingousCase.Key): passed=$dingousPassed exit=$($dingousProcess.ExitCode) timeout=$dingousTimedOut"
}
Write-Output "Relatório: $dingousFolder"
if(@($dingousResults | Where-Object {!$_.passed}).Count){throw 'Regressão Windows falhou. Consulte results.json e os logs.'}
Write-Output 'Regressão sintética Windows aprovada. Não substitui webcam/microfone reais, encerramento gráfico, Android ou integrações de produção.'
