param([int]$Camera = 0)
Set-Location $PSScriptRoot
& .\.venv\Scripts\python.exe tracker.py --camera $Camera --hands
