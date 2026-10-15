[CmdletBinding()]
param([string]$Toolchain)
$ErrorActionPreference='Stop'
$metaRoot=Split-Path $PSScriptRoot -Parent
$metaSource=Join-Path $metaRoot 'Downloads/Frameworks/MetaHumanOpenRigLogic'
$metaCommit='55756aca0c4c55adc56ad68a796bcc1c0bf6fa5a'
if(!(Test-Path $metaSource)){git clone --depth 1 --branch 5.8 https://github.com/EpicGames/OpenRigLogic.git $metaSource;if($LASTEXITCODE){throw 'Clone OpenRigLogic failed'}}
if((git -C $metaSource rev-parse HEAD) -ne $metaCommit){throw 'OpenRigLogic revision differs from the validated commit.'}
if(!$Toolchain){$Toolchain=Join-Path $metaRoot 'Downloads/Toolchains/llvm-mingw-20261006-ucrt-x86_64'}
if(!(Test-Path (Join-Path $Toolchain 'bin/clang++.exe'))){
    $metaArchive=Join-Path $metaRoot 'Downloads/llvm-mingw-20261006-ucrt-x86_64.zip'
    Invoke-WebRequest 'https://github.com/mstorsjo/llvm-mingw/releases/download/20261006/llvm-mingw-20261006-ucrt-x86_64.zip' -OutFile $metaArchive
    if((Get-FileHash $metaArchive).Hash -ne '317492C456AA27EE607A5919F1D2D38DCDC1112516A24D0BF4B00D078F52D17A'){throw 'LLVM toolchain SHA-256 mismatch'}
    Expand-Archive -LiteralPath $metaArchive -DestinationPath (Split-Path $Toolchain -Parent) -Force
}
$metaCmake=Join-Path $metaRoot 'Tracker/.venv/Scripts/cmake.exe'
$metaNinja=Join-Path $metaRoot 'Tracker/.venv/Scripts/ninja.exe'
if(!(Test-Path $metaCmake) -or !(Test-Path $metaNinja)){throw 'Install Tools/requirements-metahuman-build.txt into Tracker/.venv first.'}
$metaBuild=Join-Path $metaRoot 'Downloads/Frameworks/MetaHumanOpenRigLogic-build'
$metaOutput=Join-Path $metaRoot 'Tracker/native/MetaHuman'
New-Item -ItemType Directory -Force $metaOutput | Out-Null
$metaPreviousPath=$env:PATH
try{
    $env:PATH=(Join-Path $Toolchain 'bin')+';'+$env:PATH
    & $metaCmake -S $metaSource -B $metaBuild -G Ninja "-DCMAKE_MAKE_PROGRAM=$metaNinja" "-DCMAKE_C_COMPILER=$Toolchain/bin/clang.exe" "-DCMAKE_CXX_COMPILER=$Toolchain/bin/clang++.exe" '-DCMAKE_CXX_FLAGS=-include type_traits' -DCMAKE_BUILD_TYPE=Release -DRL_BUILD_TESTS=OFF -DRL_BUILD_BENCHMARKS=OFF -DRL_BUILD_EXAMPLES=OFF
    if($LASTEXITCODE){throw 'OpenRigLogic configure failed'}
    & $metaCmake --build $metaBuild --parallel 6
    if($LASTEXITCODE){throw 'OpenRigLogic build failed'}
    & "$Toolchain/bin/clang++.exe" -std=c++11 -O2 -static -I "$metaSource/include" "$PSScriptRoot/MetaHuman/dna_probe.cpp" "$metaBuild/libriglogic413_2_9.a" -o "$metaOutput/MetaHumanDnaProbe.exe"
    if($LASTEXITCODE){throw 'DNA preflight build failed'}
    Copy-Item "$metaSource/LICENSE" "$metaOutput/OpenRigLogic-LICENSE.txt" -Force
    Copy-Item "$Toolchain/LICENSE.txt" "$metaOutput/Toolchain-LICENSE.txt" -Force
    Copy-Item "$Toolchain/share/libc++/LICENSE.TXT" "$metaOutput/libcxx-LICENSE.txt" -Force -ErrorAction SilentlyContinue
}finally{$env:PATH=$metaPreviousPath}
Write-Output "MetaHuman DNA preflight built: $metaOutput"
