using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace WebcamAvatar
{
    /// <summary>Opt-in executable smoke test. Does not start the user's webcam.</summary>
    public class StudioPlayerSmoke : MonoBehaviour
    {
        bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--studio-smoke")>=0)new GameObject("Smoke validation").AddComponent<StudioPlayerSmoke>();
        }
        void OnEnable(){Application.logMessageReceived+=OnLog;}
        void OnLog(string message,string trace,LogType type){if(type==LogType.Exception || type==LogType.Error)failed=true;}
        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(2);
            var ui=FindAnyObjectByType<StudioUI>();
            if(ui==null){Debug.LogError("SMOKE: Studio failed to initialize");Application.Quit(1);yield break;}
            float loadingStart=Time.realtimeSinceStartup;
            while(ui.Vrm.Loading && Time.realtimeSinceStartup-loadingStart<30)yield return null;
            if(!ui.Vrm.Active){Debug.LogError("SMOKE: Default UniVRM avatar not active");Application.Quit(1);yield break;}
            if(ui.avatar.FacialBlendshapeCount!=52){Debug.LogError("SMOKE: Advanced facial rig is missing");failed=true;}
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","Validation"));Directory.CreateDirectory(root);
            yield return StartCoroutine(CheckVrm(ui,root));
            string photo=Path.Combine(root,"reference.png");
            if(File.Exists(photo)) {
                var import=(IEnumerator)ui.GetType().GetMethod("ImportPhoto",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(ui,new object[]{photo});
                yield return ui.StartCoroutine(import);
                var generate=(IEnumerator)ui.GetType().GetMethod("GenerateRoutine",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(ui,null);
                yield return ui.StartCoroutine(generate);
                if(ui.avatar.Profile.source!="photo"){Debug.LogError("SMOKE: Photo avatar not generated");failed=true;}
            }
            yield return new WaitForSecondsRealtime(.5f);
            yield return ui.StartCoroutine(ui.CaptureInterface(Path.Combine(root,"player-studio.png")));
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(768,1024),new Vector2Int(1366,768),new Vector2Int(2560,1080)}) {
                yield return ui.StartCoroutine(ui.CaptureInterface(Path.Combine(root,$"responsive-{size.x}x{size.y}.png"),size.x,size.y));
                var layout=ui.GetComponentInChildren<ResponsiveStudio>();layout.Layout(size.x,size.y);
                foreach(var button in layout.Content.GetComponentsInChildren<Button>(true)) {
                    var corners=new Vector3[4];button.GetComponent<RectTransform>().GetWorldCorners(corners);
                    for(int i=0;i<4;i++)corners[i]=layout.Content.InverseTransformPoint(corners[i]);
                    if(corners[0].x<-.5f || corners[2].x>size.x+.5f) {Debug.LogError("SMOKE: Responsive button outside content: "+button.name);failed=true;}
                }
            }
            Debug.Log("SMOKE: responsive layouts checked at 390, 768, 1366 and 2560 pixels");
            ui.avatar.Profile.style=1;
            ui.avatar.Rebuild(ui.avatar.Profile);
            ui.GetType().GetMethod("SyncAppearance",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(ui,null);
            var restored=JsonUtility.FromJson<AvatarProfile>(JsonUtility.ToJson(ui.avatar.Profile));restored.Sanitize();
            var face=ui.avatar.GetComponentInChildren<AdvancedFace>();
            if(restored.style!=1 || face.ShapeCount!=52 || face.Renderer.sharedMaterial.shader.name!="AvatarStudio/Realistic") {
                Debug.LogError("SMOKE: Realistic style persistence or facial rig failed");failed=true;
            }
            var delta=new Vector3[face.Renderer.sharedMesh.vertexCount];
            var zero=new Vector3[delta.Length];
            for(int shape=0;shape<52;shape++) {
                face.Renderer.sharedMesh.GetBlendShapeFrameVertices(shape,0,delta,zero,zero);
                float movement=0;foreach(var v in delta)movement=Mathf.Max(movement,v.sqrMagnitude);
                if(movement<.00000001f){Debug.LogError("SMOKE: Empty realistic facial shape");failed=true;}
            }
            yield return new WaitForSecondsRealtime(.5f);
            yield return ui.StartCoroutine(ui.CaptureInterface(Path.Combine(root,"player-realistic.png")));
            ui.avatar.Profile.style=0;ui.avatar.Rebuild(ui.avatar.Profile);
            if(ui.avatar.GetComponentInChildren<AdvancedFace>().Renderer.sharedMaterial.shader.name!="AvatarStudio/Cartoon") {
                Debug.LogError("SMOKE: Cartoon restoration failed");failed=true;
            }
            Debug.Log("SMOKE: both styles rendered; realistic persistence and 52 facial controls checked");
            Debug.Log("SMOKE: executable initialized, isolated avatar render and studio UI rendered; result="+(!failed));
            Application.Quit(failed?1:0);
        }
        IEnumerator CheckVrm(StudioUI ui,string root)
        {
            var vrm=ui.Vrm;var rig=vrm.Instance.Runtime.ControlRig;
            foreach(var renderer in vrm.Instance.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)
                if(material==null || material.shader==null || !material.shader.isSupported || material.shader.name=="Hidden/InternalErrorShader") {Debug.LogError("SMOKE: VRM material missing");failed=true;}
            yield return ui.StartCoroutine(ui.CaptureInterface(Path.Combine(root,"univrm-default.png")));
            ui.GetType().GetMethod("ShowPage",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(ui,new object[]{3});
            yield return ui.StartCoroutine(ui.CaptureInterface(Path.Combine(root,"univrm-model-screen.png")));
            var head=rig.GetBoneTransform(HumanBodyBones.Head);
            Quaternion before=head.rotation;
            var hand=new Point[21];hand[0]=new Point{confidence=1};
            for(int finger=0;finger<5;finger++)for(int joint=0;joint<4;joint++)hand[1+finger*4+joint]=new Point{x=(finger-2)*.017f,y=.035f+Mathf.Sin(joint*.8f)*.025f,z=(1-Mathf.Cos(joint*.8f))*.025f,confidence=1};
            var body=new Point[33];for(int i=0;i<33;i++)body[i]=new Point{confidence=1};
            body[11]=new Point{x=.3f,y=-.55f,confidence=1};body[12]=new Point{x=-.3f,y=-.55f,confidence=1};
            body[13]=new Point{x=.55f,y=-.3f,confidence=1};body[14]=new Point{x=-.55f,y=-.3f,confidence=1};
            body[15]=new Point{x=.6f,y=-.1f,confidence=1};body[16]=new Point{x=-.6f,y=-.1f,confidence=1};
            body[23]=new Point{x=.15f,confidence=1};body[24]=new Point{x=-.15f,confidence=1};
            body[25]=new Point{x=.15f,y=.4f,confidence=1};body[26]=new Point{x=-.15f,y=.3f,z=-.25f,confidence=1};
            body[27]=new Point{x=.15f,y=.78f,confidence=1};body[28]=new Point{x=-.15f,y=.65f,confidence=1};
            body[31]=new Point{x=.15f,y=.8f,z=-.18f,confidence=1};body[32]=new Point{x=-.15f,y=.65f,z=-.18f,confidence=1};
            var packet=new TrackingFrame{face=true,pose=true,points=body,leftHand=hand,rightHand=hand,rotation=new[]{.94f,0,.34f,0,1,0,-.34f,0,.94f},shapes=new[]{new Shape{name="jawOpen",value=1},new Shape{name="eyeBlinkLeft",value=1}}};
            byte[] bytes=System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(packet));
            using(var udp=new System.Net.Sockets.UdpClient()) {
                float started=Time.realtimeSinceStartup;
                while(Time.realtimeSinceStartup-started<1.5f){udp.Send(bytes,bytes.Length,"127.0.0.1",5055);yield return null;}
            }
            var expression=vrm.Instance.Runtime.Expression;
            if(expression.GetWeight(UniVRM10.ExpressionKey.CreateFromPreset(UniVRM10.ExpressionPreset.aa))<.8f || expression.GetWeight(UniVRM10.ExpressionKey.CreateFromPreset(UniVRM10.ExpressionPreset.blinkLeft))<.8f || Quaternion.Angle(head.rotation,before)<8) {
                Debug.LogError("SMOKE: VRM face/head did not respond to UDP");failed=true;
            }
            foreach(var key in new[]{HumanBodyBones.LeftIndexIntermediate,HumanBodyBones.RightIndexIntermediate,HumanBodyBones.LeftMiddleIntermediate,HumanBodyBones.RightMiddleIntermediate,HumanBodyBones.LeftRingIntermediate,HumanBodyBones.RightRingIntermediate,HumanBodyBones.LeftLittleIntermediate,HumanBodyBones.RightLittleIntermediate,HumanBodyBones.LeftThumbIntermediate,HumanBodyBones.RightThumbIntermediate}) {
                var bone=rig.GetBoneTransform(key);if(bone==null || Quaternion.Angle(bone.localRotation,Quaternion.identity)<10){Debug.LogError("SMOKE: VRM finger not driven: "+key);failed=true;}
            }
            if(Quaternion.Angle(rig.GetBoneTransform(HumanBodyBones.RightUpperLeg).localRotation,Quaternion.identity)<15){Debug.LogError("SMOKE: VRM leg not driven");failed=true;}
            yield return ui.StartCoroutine(ui.CaptureInterface(Path.Combine(root,"univrm-tracking.png")));
            yield return new WaitForSecondsRealtime(2);
            if(expression.GetWeight(UniVRM10.ExpressionKey.CreateFromPreset(UniVRM10.ExpressionPreset.aa))>.05f){Debug.LogError("SMOKE: VRM expressions did not return to neutral");failed=true;}
            string folder=Path.Combine(root,"VRMRoundtrip");Directory.CreateDirectory(folder);File.Copy(vrm.SourcePath,Path.Combine(folder,"model.vrm"),true);
            var profile=JsonUtility.FromJson<AvatarProfile>(JsonUtility.ToJson(ui.avatar.Profile));profile.vrmSource="model.vrm";File.WriteAllText(Path.Combine(folder,"avatar.json"),JsonUtility.ToJson(profile));
            var restored=JsonUtility.FromJson<AvatarProfile>(File.ReadAllText(Path.Combine(folder,"avatar.json")));
            yield return ui.StartCoroutine(ui.LoadVrmRoutine(Path.Combine(folder,restored.vrmSource)));
            if(!vrm.Active){Debug.LogError("SMOKE: Saved VRM did not reload");failed=true;}
            string prior=vrm.SourcePath;var invalid=vrm.Load(Path.Combine(root,"missing.vrm"));while(!invalid.IsCompleted)yield return null;
            if(!invalid.IsFaulted || !vrm.Active || vrm.SourcePath!=prior){Debug.LogError("SMOKE: Failed load replaced active VRM");failed=true;}
            Debug.Log("SMOKE: UniVRM default model, shaders, head, expressions, all ten fingers, leg, neutral recovery and saved model reload verified");
            ui.GetType().GetMethod("ShowPage",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(ui,new object[]{0});
        }
        void OnDisable(){Application.logMessageReceived-=OnLog;}
    }
}
