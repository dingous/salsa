param([int]$OwnerId,[string]$CommandPath)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$dingousTray=New-Object System.Windows.Forms.NotifyIcon
$dingousIcon=Join-Path $PSScriptRoot 'DingousAvatar.ico'
$dingousTray.Icon=if(Test-Path -LiteralPath $dingousIcon){New-Object System.Drawing.Icon($dingousIcon)}else{[System.Drawing.SystemIcons]::Application}
$dingousTray.Text='Dingous · agente pessoal'
$dingousMenu=New-Object System.Windows.Forms.ContextMenuStrip
function Send-DingousCommand([string]$value){
    $temporary=$CommandPath+'.tmp'
    [IO.File]::WriteAllText($temporary,$value)
    Move-Item -LiteralPath $temporary -Destination $CommandPath -Force
}
$dingousShow=$dingousMenu.Items.Add('Abrir Dingous')
$dingousShow.add_Click({Send-DingousCommand 'show'})
$dingousCorner=$dingousMenu.Items.Add('Avatar no canto inferior direito')
$dingousCorner.add_Click({Send-DingousCommand 'corner'})
$dingousFull=$dingousMenu.Items.Add('Avatar em tela cheia')
$dingousFull.add_Click({Send-DingousCommand 'full'})
$dingousPause=$dingousMenu.Items.Add('Pausar escuta')
$dingousPause.add_Click({Send-DingousCommand 'pause'})
$dingousExit=$dingousMenu.Items.Add('Encerrar Dingous')
$dingousExit.add_Click({Send-DingousCommand 'exit'})
$dingousTray.ContextMenuStrip=$dingousMenu
$dingousTray.add_DoubleClick({Send-DingousCommand 'show'})
$dingousTimer=New-Object System.Windows.Forms.Timer
$dingousTimer.Interval=2000
$dingousTimer.add_Tick({if(-not(Get-Process -Id $OwnerId -ErrorAction SilentlyContinue)){[System.Windows.Forms.Application]::ExitThread()}})
try{$dingousTray.Visible=$true;$dingousTimer.Start();[System.Windows.Forms.Application]::Run()}
finally{$dingousTimer.Dispose();$dingousTray.Dispose();$dingousMenu.Dispose()}
