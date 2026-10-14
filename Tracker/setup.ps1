param([string]$Python = 'python')
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
& $Python -m venv .venv
if ($LASTEXITCODE -ne 0) { throw 'Instale Python 3.11 ou 3.12 e informe -Python caminho\python.exe' }
& .\.venv\Scripts\python.exe -m pip install -r requirements.txt
if ($LASTEXITCODE -ne 0) { throw 'Falha ao instalar dependências.' }
& .\.venv\Scripts\python.exe tracker.py --download-only
if ($LASTEXITCODE -ne 0) { throw 'Falha ao baixar modelos.' }
