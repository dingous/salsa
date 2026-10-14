using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace WebcamAvatar
{
    public sealed class LocalJob : IDisposable
    {
        public Process Process { get; private set; }
        public readonly ConcurrentQueue<string> Output = new ConcurrentQueue<string>();
        readonly StringBuilder errors = new StringBuilder();
        public string Error { get { lock(errors) return errors.ToString(); } }
        public bool Finished => Process == null || Process.HasExited;
        public int ExitCode => Process != null && Process.HasExited ? Process.ExitCode : -1;
        public static string TrackerRoot
        {
            get
            {
                var root=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"..","Tracker"));
                if(!Directory.Exists(root)) root=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"..","..","Tracker"));
                if(!Directory.Exists(root)) root=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"..","..","..","Tracker"));
                if(!Directory.Exists(root)) throw new DirectoryNotFoundException("A pasta Tracker não foi encontrada. Use a distribuição completa do aplicativo.");
                return root;
            }
        }
        public LocalJob(string script,params string[] arguments)
        {
            if(UnityEngine.Application.platform==UnityEngine.RuntimePlatform.Android)throw new PlatformNotSupportedException("Esta função ainda requer a ponte Windows. Este APK oferece a prévia e edição dos avatares; IA, rastreamento e escuta Android ainda precisam de integração.");
            var root=TrackerRoot; string python=Path.Combine(root,".venv","Scripts","python.exe");
            if(!File.Exists(python)) throw new FileNotFoundException("Prepare o ambiente executando Tracker/setup.ps1 antes de usar o aplicativo.");
            var args=new StringBuilder("-u ").Append(Quote(Path.Combine(root,script)));
            foreach(var arg in arguments) args.Append(' ').Append(Quote(arg));
            Process=new Process {StartInfo=new ProcessStartInfo {
                FileName=python, Arguments=args.ToString(),WorkingDirectory=root,UseShellExecute=false,
                CreateNoWindow=true,RedirectStandardError=true,RedirectStandardOutput=true,
                StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8
            }};
            Process.StartInfo.EnvironmentVariables["PYTHONIOENCODING"]="utf-8";
            Process.OutputDataReceived+=(sender,e)=> {if(e.Data!=null) Output.Enqueue(e.Data);};
            Process.ErrorDataReceived+=(sender,e)=> {if(e.Data!=null) lock(errors) { if(errors.Length>24000) errors.Clear(); errors.AppendLine(e.Data); }};
            Process.Start(); Process.BeginOutputReadLine(); Process.BeginErrorReadLine();
        }
        public string FriendlyError(string fallback)
        {
            if(Process!=null && Process.HasExited) Process.WaitForExit(); // Flush async output handlers after exit.
            string[] lines=Error.Trim().Split('\n');
            for(int i=lines.Length-1;i>=0;i--) {
                string line=lines[i].Trim();
                if(line.Length==0 || line.StartsWith("W0000") || line.StartsWith("INFO:") || line.StartsWith("WARNING:")) continue;
                if(line.StartsWith("RuntimeError: ")) line=line.Substring(14);
                if(line.Length>350) line=line.Substring(0,350);
                return line;
            }
            return fallback;
        }
        public static string Quote(string value)
        {
            var output=new StringBuilder("\""); int slashes=0;
            foreach(char c in value) {
                if(c=='\\') {slashes++; continue;}
                if(c=='"') output.Append('\\',slashes*2+1).Append(c);
                else output.Append('\\',slashes).Append(c);
                slashes=0;
            }
            return output.Append('\\',slashes*2).Append('"').ToString();
        }
        public void Dispose()
        {
            if(Process==null) return;
            try { if(!Process.HasExited) {
                // The Windows venv launcher can spawn a second Python process. Close the whole owned tree.
                using(var cleanup=System.Diagnostics.Process.Start(new ProcessStartInfo {
                    FileName="taskkill.exe",Arguments="/PID "+Process.Id+" /T /F",UseShellExecute=false,
                    CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true
                })) {cleanup.WaitForExit(2000);}
                Process.WaitForExit(1000);
            }} catch(InvalidOperationException) {} catch(System.ComponentModel.Win32Exception) {}
            Process.Dispose(); Process=null;
        }
    }

    public static class PhotoPicker
    {
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        sealed class OpenFileName
        {
            public int structSize=Marshal.SizeOf(typeof(OpenFileName));
            public IntPtr owner,instance;
            public string filter="Fotos PNG e JPEG\0*.png;*.jpg;*.jpeg\0\0";
            public IntPtr customFilter;
            public int maxCustomFilter,filterIndex=1;
            public StringBuilder file=new StringBuilder(32768);
            public int maxFile=32768;
            public StringBuilder fileTitle;
            public int maxFileTitle;
            public string initialDirectory,title="Escolha uma foto frontal";
            public int flags=0x00080000|0x00001000|0x00000800|0x00000008;
            public short fileOffset,fileExtension;
            public string defaultExtension;
            public IntPtr customData,hook;
            public string template;
            public IntPtr reserved;
            public int reserved2,flagsEx;
        }
        [DllImport("comdlg32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
        static extern bool GetOpenFileName([In,Out] OpenFileName value);
        [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
        public static string Pick(bool vrm=false)
        {
            if(UnityEngine.Application.platform!=UnityEngine.RuntimePlatform.WindowsEditor && UnityEngine.Application.platform!=UnityEngine.RuntimePlatform.WindowsPlayer)
                throw new PlatformNotSupportedException("O seletor de fotos desta versão funciona no Windows.");
            var dialog=new OpenFileName {owner=GetActiveWindow()};
            if(vrm){dialog.filter="Avatar VRM\0*.vrm\0\0";dialog.title="Escolha seu avatar VRM";}
            return GetOpenFileName(dialog) ? dialog.file.ToString() : null;
        }
    }
}
