[CmdletBinding()]
param([string]$Destination)
$ErrorActionPreference='Stop'
$avatarRoot=Split-Path -Parent $PSScriptRoot
if(!$Destination){$Destination=Join-Path $avatarRoot 'Validation/AvatarDeploy'}
$target=Join-Path $Destination 'web'
New-Item -ItemType Directory -Force -Path $target | Out-Null
foreach($name in @('index.html','studio.css','studio.js','rig.js','tracking-utils.js','render-worker.js','browser-tracker.js','browser-tracking-worker.js','deployment.json')){Copy-Item -LiteralPath (Join-Path $avatarRoot "Web/$name") -Destination $target -Force}
foreach($name in @('assets','vendor')){Copy-Item -LiteralPath (Join-Path $avatarRoot "Web/$name") -Destination $target -Recurse -Force}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Dockerfile.azure-all'),(Join-Path $PSScriptRoot 'patch-nginx.cjs') -Destination $Destination -Force
# All imports are local relative URLs; an import map is unnecessary in production.
$index=Join-Path $target 'index.html'
$html=[IO.File]::ReadAllText($index)
$html=[regex]::Replace($html,'<script type="importmap">.*?</script>','')
[IO.File]::WriteAllText($index,$html,[Text.UTF8Encoding]::new($false))
Write-Output "Avatar package: $Destination"
