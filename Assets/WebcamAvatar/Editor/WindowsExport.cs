using System;
using System.IO;
using UnityEditor;

namespace WebcamAvatar.Editor
{
    [InitializeOnLoad]
    public static class WindowsExport
    {
        static WindowsExport(){BuildPlayerWindow.RegisterBuildPlayerHandler(Export);}
        static void Export(BuildPlayerOptions options)
        {
            if(options.target==BuildTarget.StandaloneWindows64){
                var output=Path.GetFullPath(options.locationPathName);
                Build(output,(options.options & BuildOptions.Development)!=0);
                if((options.options & BuildOptions.AutoRunPlayer)!=0)Run(output);
                return;
            }
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
        }
        public static void Build(string output,bool development=false)
        {
            var root=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,".."));
            var script=Path.Combine(root,"Tools","publish-windows.ps1");
            var process=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{
                FileName="powershell.exe",Arguments="-NoProfile -ExecutionPolicy Bypass -File \""+script+"\" -OutputPath \""+output+"\" -StopRunning"+(development?" -Development":""),
                WorkingDirectory=root,UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true
            });
            string errors=process.StandardError.ReadToEnd();
            process.WaitForExit();int code=process.ExitCode;process.Dispose();
            if(code!=0)throw new Exception("A exportação Windows falhou: "+(string.IsNullOrWhiteSpace(errors)?"Consulte corporate-build.log.":errors.Substring(Math.Max(0,errors.Length-1800))));
            UnityEngine.Debug.Log("WINDOWS_EXPORT_PASS: "+output+" (Cartoon + Corporativo)");
        }
        public static void Run(string output)
        {
            if(!File.Exists(output))throw new FileNotFoundException("O executável Windows exportado não foi encontrado.",output);
            var player=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{
                FileName=output,WorkingDirectory=Path.GetDirectoryName(output),UseShellExecute=true
            });
            if(player==null)throw new Exception("O Windows não conseguiu iniciar o aplicativo exportado.");
            UnityEngine.Debug.Log("WINDOWS_RUN_STARTED: "+output+" (PID "+player.Id+")");
            player.Dispose();
        }
    }
}
