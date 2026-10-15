using System;
using UnityEngine;
namespace WebcamAvatar
{
    public sealed class AndroidRuntime : MonoBehaviour
    {
        public StudioUI studio;public AvatarTracking tracking;
        public bool Listening {get;private set;} public bool Tracking {get;private set;}
        [Serializable] class NativeState {public string status,question,answer;public bool voiceReady,asrReady,speaking,inCall;public int faces;public long audioChunks,trackingFrames;}
        NativeState state=new NativeState();float nextPoll,pendingUntil;bool permissionPending;string pendingQuestion;
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
                studio.NativeReply(pendingQuestion??state.question,state.answer,state.status);
            }
        }
        void OnGUI(){if(Application.platform!=RuntimePlatform.Android)return;
            if(studio.AgentFullScreen)return;
            float scale=Mathf.Clamp(Screen.width/1000f,.8f,1.8f);GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));float y=Screen.height/scale-88;
            GUI.Box(new Rect(8,y,Screen.width/scale-16,80),"ANDROID · IA no computador / câmera e fala locais · privacidade protegida");
            if(GUI.Button(new Rect(18,y+28,160,36),"Parear IA local"))using(var activity=Activity)Call("pair",activity);
            if(GUI.Button(new Rect(188,y+28,160,36),"Testar voz local")){if(!Listening&&!Tracking){Tracking=true;Apply();}using(var activity=Activity)Call("preview",activity);}
            if(GUI.Button(new Rect(358,y+28,190,36),Listening?"Parar escuta":"Escutar Dingous"))SetListening(!Listening);
            if(GUI.Button(new Rect(558,y+28,160,36),Tracking?"Parar câmera":"Rastrear câmera"))SetTracking(!Tracking);
            GUI.Label(new Rect(730,y+28,Screen.width/scale-740,42),$"Frames: {state.trackingFrames} · áudio: {state.audioChunks}\nRostos: {state.faces} · ASR: {state.asrReady} · voz: {state.voiceReady}");GUI.matrix=Matrix4x4.identity;
        }
        void OnApplicationQuit(){using(var activity=Activity)Call("stop",activity);}
    }
}
