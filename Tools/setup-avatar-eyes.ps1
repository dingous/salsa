$ErrorActionPreference='Stop'
$eyesRoot=Split-Path $PSScriptRoot -Parent
$eyesModel=Join-Path $eyesRoot 'Tracker/models/yolox_nano.onnx'
New-Item -ItemType Directory -Force (Split-Path $eyesModel) | Out-Null
Invoke-WebRequest 'https://github.com/Megvii-BaseDetection/YOLOX/releases/download/0.1.1rc0/yolox_nano.onnx' -OutFile $eyesModel
if((Get-FileHash $eyesModel).Hash -ne 'C789161ED43C8269FCD4E67C67EEEB4E80C622DA2EB296A20BC6007BD18A0B7D'){throw 'Checksum YOLOX inválido.'}
Write-Output 'YOLOX Nano pronto para inferência local (CPU).'
