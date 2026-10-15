using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace WebcamAvatar
{
    /// <summary>Owns the application's tray lifecycle and companion window.</summary>
    public sealed class WindowsAgentPresence : MonoBehaviour
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr window,int command);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr window);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
        [DllImport("user32.dll")] static extern bool SystemParametersInfo(uint action,uint parameter,out WorkArea area,uint flags);
        [StructLayout(LayoutKind.Sequential)] struct WorkArea {public int left,top,right,bottom;}
        Process tray;string commandPath;bool exiting;StudioUI studio;IntPtr window;
        IntPtr Window {get {if(window==IntPtr.Zero)window=Process.GetCurrentProcess().MainWindowHandle;return window;}}
        void Start(){
            studio=GetComponent<StudioUI>();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--corporate-smoke")>=0)return;
            string directory=Path.Combine(Application.persistentDataPath,"Presence",Process.GetCurrentProcess().Id.ToString());
            Directory.CreateDirectory(directory);commandPath=Path.Combine(directory,"command.txt");
            string script=Path.Combine(Application.streamingAssetsPath,"Dingous","tray.ps1");
            try{tray=Process.Start(new ProcessStartInfo("powershell.exe","-NoProfile -STA -File \""+script+"\" -OwnerId "+Process.GetCurrentProcess().Id+" -CommandPath \""+commandPath+"\""){UseShellExecute=false,CreateNoWindow=true});}
            catch(Exception e){studio.SetStatus("Não foi possível abrir o ícone Dingous: "+e.Message,true);}
            Application.wantsToQuit+=WantsToQuit;
        }
        bool WantsToQuit(){if(exiting||studio==null||!studio.AgentMode||tray==null||tray.HasExited)return true;ShowWindow(Window,0);return false;}
        public void Corner(){
            StartCoroutine(PlaceCorner());
        }
        System.Collections.IEnumerator PlaceCorner(){
            Screen.SetResolution(380,540,FullScreenMode.Windowed);
            yield return null;yield return null;
            if(SystemParametersInfo(48,0,out var area,0))SetWindowPos(Window,new IntPtr(-1),area.right-400,area.bottom-580,396,578,0x0040);
        }
        public void Hide(){ShowWindow(Window,0);}
        void Update(){
            if(commandPath==null||!File.Exists(commandPath))return;
            string command;
            try{command=File.ReadAllText(commandPath).Trim();File.Delete(commandPath);}catch(IOException){return;}
            if(command=="exit"){exiting=true;Application.Quit();return;}
            if(command=="pause"){studio.PauseAgent();return;}
            ShowWindow(Window,9);SetForegroundWindow(Window);
            if(command=="corner")Corner();
            else if(command=="full"){Screen.SetResolution(Display.main.systemWidth,Display.main.systemHeight,FullScreenMode.FullScreenWindow);}
        }
        void OnDestroy(){Application.wantsToQuit-=WantsToQuit;try{if(tray!=null&&!tray.HasExited)tray.Kill();}catch(InvalidOperationException){} }
#else
        public void Corner(){}
        public void Hide(){}
#endif
    }
}
