using System;
using UnityEngine;
namespace WebcamAvatar
{
    public sealed class AndroidRuntime : MonoBehaviour
    {
        public StudioUI studio;public AvatarTracking tracking;
        public bool Listening {get;private set;} public bool Tracking {get;private set;}
        [Serializable] class NativeState {public string status,question,answer;public bool voiceReady,asrReady,speaking,inCall,listening,tracking;public int faces;public long audioChunks,trackingFrames;}
        NativeState state=new NativeState();float nextPoll,pendingUntil;bool permissionPending;string pendingQuestion;
        public static float ControlsHeight=>Screen.width<600?184:112;
        AndroidJavaObject Activity {get {using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))return player.GetStatic<AndroidJavaObject>("currentActivity");}}
        void Call(string method,params object[] args){using(var bridge=new AndroidJavaClass("br.com.dingous.avatar.DingousNative"))bridge.CallStatic(method,args);}
        void Start(){if(Debug.isDebugBuild){using(var activity=Activity)using(var intent=activity.Call<AndroidJavaObject>("getIntent")){string token=intent.Call<string>("getStringExtra","dingous_pair_token"),url=intent.Call<string>("getStringExtra","dingous_pair_url");if(!string.IsNullOrEmpty(token)&&!string.IsNullOrEmpty(url))Call("configure",activity,url,token);}}}
        void Apply(){
            if(permissionPending)return;
            if(!Listening&&!Tracking){using(var activity=Activity)Call("stop",activity);return;}
            var permissions=new System.Collections.Generic.List<string>();
            if(!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone)&&Listening)permissions.Add(UnityEngine.Android.Permission.Microphone);
            if(!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera)&&Tracking)permissions.Add(UnityEngine.Android.Permission.Camera);
            if(permissions.Count>0){permissionPending=true;int remaining=permissions.Count;var callbacks=new UnityEngine.Android.PermissionCallbacks();callbacks.PermissionGranted+=name=>{if(--remaining==0){permissionPending=false;Apply();}};callbacks.PermissionDenied+=name=>{permissionPending=false;Listening=Tracking=false;studio.SetStatus("Permissão negada. Microfone/câmera não iniciados.",true);};UnityEngine.Android.Permission.RequestUserPermissions(permissions.ToArray(),callbacks);return;}
            using(var activity=Activity)Call("start",activity,Listening,Tracking);
        }
        public void SetListening(bool enabled){Listening=enabled;if(enabled)Tracking=true;Apply();}
        public void SetTracking(bool enabled){Tracking=enabled;Apply();if(!enabled)tracking.ClearSource();}
        public void ChooseVoice(){using(var activity=Activity)Call("chooseVoice",activity);}
        public void Ask(string question){
            if(string.IsNullOrWhiteSpace(question))return;
            if(!string.IsNullOrEmpty(pendingQuestion)){studio.SetStatus("Aguarde o envio da pergunta pendente.",true);return;}
            pendingQuestion=question;pendingUntil=Time.unscaledTime+150;
            if(!Listening&&!Tracking){Tracking=true;Apply();}
            studio.SetStatus("Preparando o envio para a IA local…");
        }
        void Update(){
            if(Application.platform!=RuntimePlatform.Android||Time.unscaledTime<nextPoll)return;nextPoll=Time.unscaledTime+.5f;
            using(var activity=Activity)using(var bridge=new AndroidJavaClass("br.com.dingous.avatar.DingousNative")){
                if(!string.IsNullOrEmpty(pendingQuestion)&&!permissionPending){
                    if(Time.unscaledTime>pendingUntil||(!Listening&&!Tracking)){pendingQuestion=null;studio.SetStatus("Pergunta não enviada. Confira as permissões e tente novamente.",true);}
                    else if(bridge.CallStatic<bool>("ask",activity,pendingQuestion))pendingQuestion=null;
                }
                string json=bridge.CallStatic<string>("state",activity);if(json=="{}")return;
                state=JsonUtility.FromJson<NativeState>(json);tracking.NativeSpeaking=state.speaking;
                if(!permissionPending){Listening=state.listening;Tracking=state.tracking;}
                studio.NativeReply(pendingQuestion??state.question,state.answer,state.status);
            }
        }
        void OnGUI(){if(Application.platform!=RuntimePlatform.Android)return;
            if(studio.AgentFullScreen)return;
            var safe=Screen.safeArea;bool compact=safe.width<600;
            float height=ControlsHeight,y=Screen.height-safe.y-height,width=safe.width-24,x=safe.x+12;
            GUI.Box(new Rect(x,y,width,height-8),"ANDROID · SERVIÇOS LOCAIS");
            int columns=compact?2:4;float gap=8,w=(width-24-gap*(columns-1))/columns;
            if(GUI.Button(new Rect(x+12,y+28,w,40),"Parear IA"))using(var activity=Activity)Call("pair",activity);
            if(GUI.Button(new Rect(x+12+w+gap,y+28,w,40),state.voiceReady?"Testar voz":"Instalar voz pt-BR")){
                using(var activity=Activity){if(!state.voiceReady)Call("installVoice",activity);else{if(!Listening&&!Tracking){Tracking=true;Apply();}Call("preview",activity);}}
            }
            float secondY=compact?y+76:y+28,secondX=compact?x+12:x+12+2*(w+gap);
            if(GUI.Button(new Rect(secondX,secondY,w,40),Listening?"Parar escuta":"Escutar Dingous"))SetListening(!Listening);
            if(GUI.Button(new Rect(secondX+w+gap,secondY,w,40),Tracking?"Parar câmera":"Rastrear câmera"))SetTracking(!Tracking);
            GUI.Label(new Rect(x+12,compact?y+124:y+74,width-24,40),$"Frames: {state.trackingFrames} · áudio: {state.audioChunks} · rostos: {state.faces}\nReconhecimento: {state.asrReady} · voz pt-BR: {state.voiceReady}");
        }
        // Explicit pause/Webcam/notification stop controls own the foreground service.
        // Closing the Unity activity must not stop a user-started background listener.
    }
}
