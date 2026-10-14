[CmdletBinding()]
param([string]$Destination)
$ErrorActionPreference='Stop'
$avatarRoot=Split-Path -Parent $PSScriptRoot
if(!$Destination){$Destination=Join-Path $avatarRoot 'Validation/AvatarDeploy'}
$target=Join-Path $Destination 'web'
New-Item -ItemType Directory -Force -Path $target | Out-Null
foreach($name in @('index.html','studio.css','studio.js','rig.js','pose-filter.js','tracking-utils.js','render-worker.js','makehuman-appearance.js','browser-tracker.js','browser-tracking-worker.js','appearance-storage.js','deployment.json')){Copy-Item -LiteralPath (Join-Path $avatarRoot "Web/$name") -Destination $target -Force}
foreach($name in @('assets','vendor')){Copy-Item -LiteralPath (Join-Path $avatarRoot "Web/$name") -Destination $target -Recurse -Force}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Dockerfile.azure-all'),(Join-Path $PSScriptRoot 'patch-nginx.cjs') -Destination $Destination -Force
# All imports are local relative URLs; an import map is unnecessary in production.
$index=Join-Path $target 'index.html'
$html=[IO.File]::ReadAllText($index)
$html=[regex]::Replace($html,'<script type="importmap">.*?</script>','')
[IO.File]::WriteAllText($index,$html,[Text.UTF8Encoding]::new($false))
$manifestPath=Join-Path $target 'deployment.json'
$manifestData=[IO.File]::ReadAllText($manifestPath) | ConvertFrom-Json
foreach($entry in $manifestData.files.PSObject.Properties){$entry.Value=(Get-FileHash -LiteralPath (Join-Path $target $entry.Name) -Algorithm SHA256).Hash.ToLowerInvariant()}
[IO.File]::WriteAllText($manifestPath,($manifestData | ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
Write-Output "Avatar package: $Destination"
