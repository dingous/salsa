using System;
using UnityEngine;

namespace WebcamAvatar
{
    public partial class StudioUI
    {
        bool sessionLocked,sessionAuthenticated;
        string sessionMessage="Verificando sessão salva…";
        LocalJob sessionJob;
        LocalJob localAdminJob;
        void BeginSessionLogin(){
            if(localAdminJob!=null||sessionJob!=null)return;
            if(Application.isEditor||Debug.isDebugBuild){
                try{sessionMessage="Preparando o ChatTrade local…";localAdminJob=new LocalJob("local_chattrade_admin.py","--start");}
                catch(Exception e){sessionMessage=e.Message;}
            }else SessionJob("--login");
        }
        bool SessionTesting=>Array.IndexOf(Environment.GetCommandLineArgs(),"--session-ui")>=0;
        void StartSessionGate(){
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--corporate-smoke")>=0&&!SessionTesting)return;
            sessionLocked=true;canvas.enabled=false;
            if(PlayerPrefs.GetInt("DingousLoggedOut",0)==1){sessionMessage="Entre para acessar seus agentes Dingous.";return;}
            SessionJob();
        }
        void SessionJob(params string[] args){
            if(sessionJob!=null)return;
            try{sessionMessage=args.Length>0&&args[0]=="--login"?"Conclua o login no navegador. Aguardando autorização…":"Verificando sessão salva…";sessionJob=new LocalJob("avatar_session.py",args);}
            catch(Exception){sessionMessage="Não foi possível iniciar o login nesta plataforma.";}
        }
        void UpdateSession(){
            if(localAdminJob!=null){
                while(localAdminJob.Output.TryDequeue(out string progress))if(progress.Contains("|"))sessionMessage=progress.Substring(progress.IndexOf('|')+1);
                if(localAdminJob.Finished){
                    bool ready=localAdminJob.ExitCode==0;localAdminJob.Dispose();localAdminJob=null;
                    if(ready)SessionJob("--login");
                }
            }
            if(sessionJob==null)return;
            bool authenticated=false;
            while(sessionJob.Output.TryDequeue(out string line)){
                if(line.StartsWith("AUTH_OK|"))authenticated=true;
                if(line.StartsWith("AUTH_REQUIRED|")||line.StartsWith("AUTH_LOGGED_OUT|"))sessionMessage=line.Substring(line.IndexOf('|')+1);
            }
            if(authenticated){
                sessionAuthenticated=true;sessionLocked=false;canvas.enabled=true;
                PlayerPrefs.SetInt("DingousLoggedOut",0);PlayerPrefs.Save();
                if(PlayerPrefs.GetInt("DingousAgentAvailable",0)==1)StartCoroutine(ResumeAgentMode());
            }
            if(sessionJob.Finished){
                if(sessionJob.ExitCode!=0)sessionMessage=sessionJob.FriendlyError("Login não concluído. Confira se o admin e a API ChatTrade estão abertos.");
                sessionJob.Dispose();sessionJob=null;
            }
        }
        void LogoutSession(){
            localAdminJob?.Dispose();localAdminJob=null;
            sessionJob?.Dispose();sessionJob=null;
            SetVoiceListening(false);StopPartner();StopLaboratory();speechPlayback?.Stop();
            eyesJob?.Dispose();eyesJob=null;trackerJob?.Dispose();trackerJob=null;StopCamera();
            if(androidRuntime!=null)androidRuntime.SetTracking(false);
            partnerMode=false;tracking.AgentMode=false;tracking.ClearSource();agentMotion.IdleEnabled=false;
            PlayerPrefs.SetInt("DingousLoggedOut",1);PlayerPrefs.SetInt("DingousAgentAvailable",0);PlayerPrefs.Save();
            if(presentation)SetPresentation(false);
            sessionAuthenticated=false;sessionLocked=true;canvas.enabled=false;ttsOpen=false;
            SessionJob("--logout");
        }
        bool DrawSession(){
            GUI.matrix=Matrix4x4.identity;
            if(!sessionLocked){
                if(!AgentFullScreen&&GUI.Button(new Rect(Mathf.Max(8,Screen.width-128),12,112,34),"Deslogar"))LogoutSession();
                return false;
            }
            GUI.depth=-100;
            GUI.color=background;GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;
            float w=Mathf.Min(460,Screen.width-32),h=Mathf.Min(350,Screen.height-24);
            float x=(Screen.width-w)/2,y=(Screen.height-h)/2;
            GUI.color=panel;GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=purple;GUI.DrawTexture(new Rect(x,y,w,3),Texture2D.whiteTexture);GUI.color=Color.white;
            var heading=new GUIStyle(GUI.skin.label){fontSize=26,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            GUI.Label(new Rect(x+16,y+20,w-32,52),"DINGOUS AVATAR",heading);
            var text=new GUIStyle(GUI.skin.label){wordWrap=true,alignment=TextAnchor.MiddleCenter};
            GUI.Label(new Rect(x+24,y+80,w-48,64),sessionMessage,text);
            var button=new GUIStyle(GUI.skin.button){fontSize=15,fontStyle=FontStyle.Bold};
            button.normal.background=Texture2D.whiteTexture;button.normal.textColor=Color.white;
            button.hover.background=Texture2D.whiteTexture;button.hover.textColor=Color.white;
            button.active.background=Texture2D.whiteTexture;button.active.textColor=Color.white;
            GUI.enabled=sessionJob==null&&localAdminJob==null;
            GUI.backgroundColor=purple;
            if(GUI.Button(new Rect(x+24,y+156,w-48,48),"Entrar com ChatTrade",button))BeginSessionLogin();
            GUI.backgroundColor=field;
            if(GUI.Button(new Rect(x+24,y+216,w-48,40),"Usar somente Webcam",button)){
                sessionLocked=false;sessionAuthenticated=false;canvas.enabled=true;SelectRunMode(false);
            }
            GUI.enabled=true;GUI.backgroundColor=Color.white;
            string environment=Application.isEditor||Debug.isDebugBuild?"Desenvolvimento · API localhost":"Produção · dingous.com.br";
            GUI.Label(new Rect(x+24,y+264,w-48,24),environment,text);
            GUI.Label(new Rect(x+24,y+290,w-48,52),"Login pelo navegador, com sugestões do seu gerenciador de senhas. Apenas a sessão fica protegida no Windows.",text);
            return true;
        }
    }
}
