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
            if(options.target==BuildTarget.StandaloneWindows64){Build(Path.GetFullPath(options.locationPathName));return;}
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
        }
        public static void Build(string output)
        {
            var root=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,".."));
            var script=Path.Combine(root,"Tools","publish-windows.ps1");
            var process=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{
                FileName="powershell.exe",Arguments="-NoProfile -ExecutionPolicy Bypass -File \""+script+"\" -OutputPath \""+output+"\"",
                WorkingDirectory=root,UseShellExecute=false,CreateNoWindow=true
            });
            process.WaitForExit();int code=process.ExitCode;process.Dispose();
            if(code!=0)throw new Exception("A exportação Windows falhou. Consulte corporate-build.log.");
            UnityEngine.Debug.Log("WINDOWS_EXPORT_PASS: "+output+" (Cartoon + Corporativo)");
        }
    }
}
