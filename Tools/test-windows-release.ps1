[CmdletBinding()]
param(
    [string]$Executable,
    [ValidateRange(30,1800)][int]$TimeoutSeconds=240
)
$ErrorActionPreference='Stop'
$dingousRoot=Split-Path $PSScriptRoot -Parent
if(!$Executable){$Executable=Join-Path $dingousRoot 'publish/Windows/AvatarStudio.exe'}
$Executable=[IO.Path]::GetFullPath($Executable)
if(!(Test-Path -LiteralPath $Executable)){throw "Executável ausente: $Executable"}
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
}
foreach($dingousCase in $dingousCases.GetEnumerator()){
    $dingousLog=Join-Path $dingousFolder ($dingousCase.Key+'.log')
    $dingousStarted=Get-Date
    $dingousArguments=@('-batchmode','-nographics','--corporate-smoke','-logFile',('"'+$dingousLog+'"'))
    if($dingousCase.Key -eq 'avaturn'){
        $dingousArguments+=@('--avaturn-file',('"'+(Join-Path $dingousRoot 'Web/assets/AvaturnBase.glb')+'"'))
    }elseif($dingousCase.Key -eq 'metahuman-fixture'){
        # Existing MakeHuman exercises the prepared-import path; no MetaHuman art asset is bundled.
        $dingousArguments+=@('--metahuman-file',('"'+(Join-Path $dingousRoot 'Web/assets/CorporateHuman.vrm')+'"'))
    }else{$dingousArguments+='--'+$dingousCase.Key}
    $dingousProcess=Start-Process -FilePath $Executable -WorkingDirectory $dingousRoot -ArgumentList $dingousArguments -WindowStyle Hidden -PassThru
    $dingousTimedOut=!$dingousProcess.WaitForExit($TimeoutSeconds*1000)
    if($dingousTimedOut){$dingousProcess.Kill();$dingousProcess.WaitForExit()}
    $dingousText=if(Test-Path -LiteralPath $dingousLog){Get-Content -LiteralPath $dingousLog -Raw}else{''}
    $dingousPassed=!$dingousTimedOut -and $dingousProcess.ExitCode -eq 0 -and $dingousText.Contains($dingousCase.Value) -and !$dingousText.Contains('CORPORATE_FAIL:')
    $dingousResults+=[pscustomobject]@{test=$dingousCase.Key;passed=$dingousPassed;exitCode=$dingousProcess.ExitCode;timedOut=$dingousTimedOut;seconds=[Math]::Round(((Get-Date)-$dingousStarted).TotalSeconds,1);log=$dingousLog}
    $dingousResults | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $dingousFolder 'results.json') -Encoding UTF8
    Write-Output "$($dingousCase.Key): passed=$dingousPassed exit=$($dingousProcess.ExitCode) timeout=$dingousTimedOut"
}
Write-Output "Relatório: $dingousFolder"
if(@($dingousResults | Where-Object {!$_.passed}).Count){throw 'Regressão Windows falhou. Consulte results.json e os logs.'}
Write-Output 'Regressão sintética Windows aprovada. Não substitui webcam/microfone reais, encerramento gráfico, Android ou integrações de produção.'
