$ErrorActionPreference='Stop'
$dingousRoot=Split-Path $PSScriptRoot -Parent
$dingousModels=Join-Path $dingousRoot 'CorporateUnity/Assets/StreamingAssets/AndroidModels'
New-Item -ItemType Directory -Force $dingousModels | Out-Null
$dingousTasks=@{
 'face.task'='https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/1/face_landmarker.task'
 'pose.task'='https://storage.googleapis.com/mediapipe-models/pose_landmarker/pose_landmarker_lite/float16/1/pose_landmarker_lite.task'
 'hand.task'='https://storage.googleapis.com/mediapipe-models/hand_landmarker/hand_landmarker/float16/1/hand_landmarker.task'
}
foreach($dingousName in $dingousTasks.Keys){$dingousTarget=Join-Path $dingousModels $dingousName;if(!(Test-Path -LiteralPath $dingousTarget)){Invoke-WebRequest $dingousTasks[$dingousName] -OutFile $dingousTarget}}
if(!(Test-Path -LiteralPath (Join-Path $dingousModels 'vosk-model-small-pt-0.3'))){
 $dingousArchive=Join-Path $dingousRoot 'Validation/vosk-model-small-pt-0.3.zip'
 New-Item -ItemType Directory -Force (Split-Path $dingousArchive) | Out-Null
 Invoke-WebRequest 'https://alphacephei.com/vosk/models/vosk-model-small-pt-0.3.zip' -OutFile $dingousArchive
 Add-Type -AssemblyName System.IO.Compression.FileSystem
 $dingousZip=[IO.Compression.ZipFile]::OpenRead($dingousArchive)
 try{foreach($dingousEntry in $dingousZip.Entries){$dingousResolved=[IO.Path]::GetFullPath((Join-Path $dingousModels $dingousEntry.FullName));if(!$dingousResolved.StartsWith([IO.Path]::GetFullPath($dingousModels)+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Caminho inválido no pacote de modelos'}}}finally{$dingousZip.Dispose()}
 Expand-Archive -LiteralPath $dingousArchive -DestinationPath $dingousModels -Force
}
