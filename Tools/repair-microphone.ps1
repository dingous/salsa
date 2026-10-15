$ErrorActionPreference='Stop'
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
$principal=[Security.Principal.WindowsPrincipal]::new($identity)
if(!$principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){
    throw 'Abra o PowerShell como administrador e execute este script novamente.'
}
$devices=@(Get-PnpDevice -Class MEDIA | Where-Object {$_.FriendlyName -eq 'Tecnologia Intel® Smart Sound para microfones digitais'})
if($devices.Count -ne 1){throw 'Microfone Intel Smart Sound não identificado unicamente; nenhum dispositivo foi alterado.'}
& pnputil.exe /restart-device $devices[0].InstanceId
if($LASTEXITCODE -ne 0){throw 'O Windows não confirmou a reinicialização do microfone.'}
Write-Host 'Confira a mensagem do Windows acima. Reabra a escuta do Dingous e teste o microfone.'
