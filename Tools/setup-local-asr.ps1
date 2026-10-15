$ErrorActionPreference='Stop'
$asrRoot=Split-Path $PSScriptRoot -Parent
$asrPython=Join-Path $asrRoot 'Tracker/.venv/Scripts/python.exe'
if(!(Test-Path -LiteralPath $asrPython)){throw 'Prepare o ambiente Python do Tracker antes de instalar o reconhecimento local.'}
& $asrPython -c 'import sys; from faster_whisper import WhisperModel; WhisperModel("small",device="cpu",compute_type="int8",download_root=sys.argv[1],cpu_threads=4); print("Whisper small local instalado.")' (Join-Path $asrRoot 'Tracker/models/whisper')
if($LASTEXITCODE -ne 0){throw 'Não foi possível instalar o modelo de reconhecimento local.'}
