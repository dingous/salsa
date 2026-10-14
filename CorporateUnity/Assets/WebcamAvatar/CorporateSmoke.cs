using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using UniVRM10;
using UnityEngine;
using UnityEngine.UI;

namespace WebcamAvatar
{
    public sealed class CorporateSmoke : MonoBehaviour
    {
        bool failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--corporate-smoke")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"--corporate-live-e2e")>=0)new GameObject("Corporate smoke").AddComponent<CorporateSmoke>();}
        void OnEnable(){Application.logMessageReceived+=Log;}
        void Log(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception)failed=true;}
        void Check(bool ok,string message){if(!ok){failed=true;Debug.LogError("CORPORATE_FAIL: "+message);}else Debug.Log("CORPORATE_PASS: "+message);}
        static void Call(StudioUI ui,string name,params object[] args)=>ui.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ui,args);
        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(3);
            var ui=FindAnyObjectByType<StudioUI>();
            if(ui==null){Application.Quit(1);yield break;}
            float start=Time.realtimeSinceStartup;
            while(ui.Vrm.Loading && Time.realtimeSinceStartup-start<45)yield return null;
            Check(ui.Vrm.Active,"MakeHuman default loaded by UniVRM");
            var filter=new AdaptivePoseFilter();float rawEnergy=0,filteredEnergy=0;Point[] stable=null;
            for(int sample=0;sample<120;sample++){
                var points=new Point[33];float noise=.005f*Mathf.Sin(sample*2*Mathf.PI/3);
                for(int i=0;i<33;i++)points[i]=P(.5f+noise,0);
                stable=filter.Process(points,sample/30f);
                if(sample>30){rawEnergy+=noise*noise;filteredEnergy+=(stable[0].x-.5f)*(stable[0].x-.5f);}
            }
            Check(filteredEnergy<rawEnergy*.25f,"One Euro suppresses resting jitter energy by at least 75 percent");
            for(int sample=120;sample<135;sample++){
                var points=new Point[33];for(int i=0;i<33;i++)points[i]=P(.7f,0);
                stable=filter.Process(points,sample/30f);
            }
            Check(Mathf.Abs(stable[0].x-.7f)<.02f,"One Euro follows intentional 20 cm movement within 500 ms");
            Check(filter.Process(null,4.5f)==stable,"Brief landmark dropout holds the last observation");
            Check(filter.Process(null,4.8f)==null,"Long landmark dropout releases held observation");
            foreach(var target in new[]{new Vector3(.15f,.45f,-.1f),new Vector3(-.15f,.45f,-.1f),new Vector3(0,-.4f,-.25f)}) {
                VrmAvatarController.SolveArm(target,new Vector3(.3f,.1f,0),.32f,.28f,out var upper,out var lower);
                Check(Vector3.Distance(upper*.32f+lower*.28f,target)<.001f,"IK reaches seated/head target "+target);
                Check(Vector3.Angle(upper,lower)<=150.01f,"IK elbow limit "+target);
            }
            if(!ui.Vrm.Active){Application.Quit(1);yield break;}
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","Validation"));Directory.CreateDirectory(root);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--corporate-live-e2e")>=0) {
                bool gestures=Array.IndexOf(Environment.GetCommandLineArgs(),"--gestures")>=0;
                var audit=new GestureAudit();
                Call(ui,"ToggleTracking");Call(ui,"ToggleCamera");
                float connectedAt=-1;bool cameraReady=false,clean=false,faceSeen=false,poseSeen=false,handsSeen=false;start=Time.realtimeSinceStartup;
                while(Time.realtimeSinceStartup-start<(gestures?75:30)) {
                    if(ui.tracking.Connected && connectedAt<0){connectedAt=Time.realtimeSinceStartup;Debug.Log("CORPORATE_LIVE_TRACKER_READY");}
                    if(!cameraReady && ui.GetComponent<VirtualCameraOutput>().Sending){cameraReady=true;Debug.Log("CORPORATE_LIVE_CAMERA_READY");}
                    faceSeen|=ui.tracking.FaceDetected;poseSeen|=ui.tracking.PoseDetected;handsSeen|=ui.tracking.HandsDetected;
                    if(gestures)audit.Sample(ui);
                    if(!gestures&&!clean && Time.realtimeSinceStartup-start>15){clean=true;ui.avatar.previewAnimation=true;ui.avatar.previewExpressionPreset=4;Call(ui,"SetPresentation",true);}
                    yield return null;
                }
                Check(connectedAt>=0,"Physical webcam produced valid tracking packets");
                if(gestures){audit.Save(Path.Combine(root,"corporate-gesture-audit.json"));Check(audit.Passed,"Real webcam head reach, arms and finger curl on both sides");}
                Debug.Log($"CORPORATE_LIVE_DETECTIONS face={faceSeen} pose={poseSeen} hands={handsSeen}");
                var broadcaster=ui.GetComponent<VirtualCameraOutput>();
                Check(broadcaster.Sending && broadcaster.Error==null,"Unity camera connected to OBS bridge on TCP 5060");
                Call(ui,"ToggleTracking");Call(ui,"StopCamera");
                File.WriteAllText(Path.Combine(root,"corporate-live-result.txt"),failed?"FAIL":"PASS");
                Debug.Log("CORPORATE_LIVE_RESULT="+(!failed));Application.Quit(failed?1:0);yield break;
            }
            var vrm=ui.Vrm;var appearance=vrm.Appearance;
            yield return new WaitForSecondsRealtime(.5f);
            Check(appearance!=null && appearance.IsCorporateHuman,"Actual MakeHuman identity morphs available");
            Check(vrm.ExpressionCount>=67,"52 ARKit expressions and 15 visemes");
            Call(ui,"ShowPage",3);
            yield return ui.StartCoroutine(ui.CaptureInterface(Path.Combine(root,"corporate-realistic.png")));
            foreach(var mesh in vrm.Instance.GetComponentsInChildren<Renderer>())foreach(var material in mesh.sharedMaterials)
                Check(material.shader!=null && material.shader.isSupported && material.shader.name!="Hidden/InternalErrorShader","Material "+material.name);
            ui.avatar.Profile.style=0;ui.avatar.Profile.hairStyle=1;ui.avatar.Profile.glasses=true;ui.avatar.Profile.beard=true;
            vrm.ApplyAppearance();Call(ui,"SyncAppearance");yield return null;
            Check(Enabled(vrm,"short04") && !Enabled(vrm,"short02") && !Enabled(vrm,"long01"),"Fitted hair selection");
            Check(Enabled(vrm,"beard") && Enabled(vrm,"glasses"),"Beard and glasses selection");
            Check(vrm.Instance.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterials[0].shader.name=="CorporateAvatar/TexturedToon","Stylized textured shader");
            yield return ui.StartCoroutine(ui.CaptureInterface(Path.Combine(root,"corporate-stylized.png")));
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(768,1024),new Vector2Int(1366,768),new Vector2Int(2560,1080)}) {
                yield return ui.StartCoroutine(ui.CaptureInterface(Path.Combine(root,$"corporate-{size.x}.png"),size.x,size.y));
                var layout=ui.GetComponentInChildren<ResponsiveStudio>();layout.Layout(size.x,size.y);
                foreach(var button in layout.Content.GetComponentsInChildren<Button>(true)) {
                    var corners=new Vector3[4];button.GetComponent<RectTransform>().GetWorldCorners(corners);
                    for(int i=0;i<4;i++)corners[i]=layout.Content.InverseTransformPoint(corners[i]);
                    Check(corners[0].x>=-.5f && corners[2].x<=size.x+.5f,"Responsive "+size.x+" "+button.name);
                }
            }
            ui.avatar.Profile.style=1;ui.avatar.Profile.hairStyle=0;ui.avatar.Profile.beard=false;ui.avatar.Profile.glasses=false;vrm.ApplyAppearance();
            var expression=vrm.Instance.Runtime.Expression;
            ui.tracking.mirror=false;
            var jaw=ExpressionKey.CreateCustom("jawOpen");var blink=ExpressionKey.CreateCustom("eyeBlinkLeft");
            var body=new Point[33];for(int i=0;i<33;i++)body[i]=new Point{confidence=1};
            body[11]=P(.3f,-.55f);body[12]=P(-.3f,-.55f);body[13]=P(.58f,-.45f);body[14]=P(-.58f,-.45f);
            body[15]=P(.66f,-.70f);body[16]=P(-.66f,-.70f);body[23]=P(.15f,0);body[24]=P(-.15f,0);
            body[25]=P(.15f,.4f);body[26]=P(-.15f,.4f);body[27]=P(.15f,.78f);body[28]=P(-.15f,.78f);
            body[31]=P(.15f,.8f,-.18f);body[32]=P(-.15f,.8f,-.18f);
            var packet=new TrackingFrame{face=true,pose=true,points=body,rotation=new[]{1f,0,0,0,1f,0,0,0,1f},shapes=new[]{new Shape{name="jawOpen",value=.9f},new Shape{name="eyeBlinkLeft",value=.9f}}};
            using(var udp=new UdpClient()) {
                byte[] bytes=System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(packet));start=Time.realtimeSinceStartup;
                while(Time.realtimeSinceStartup-start<1.5f){udp.Send(bytes,bytes.Length,"127.0.0.1",ui.tracking.port);yield return null;}
            }
            Check(expression.GetWeight(jaw)>.8f && expression.GetWeight(blink)>.8f,"UDP drives independent ARKit face controls");
            using(var udp=new UdpClient()) {
                foreach(bool mirror in new[]{false,true}) {
                    ui.tracking.mirror=mirror;
                    for(int side=0;side<2;side++)for(int direction=0;direction<6;direction++) {
                        int source=mirror?1-side:side;int shoulder=source==0?11:12,elbow=source==0?13:14,wrist=source==0?15:16;
                        Vector3 desired=direction==0?new Vector3(side==0?1:-1,0,0):direction==1?Vector3.up:direction==2?Vector3.down:direction==3?Vector3.back:direction==4?Vector3.forward:new Vector3(side==0?1:-1,1,0).normalized;
                        body[elbow]=P(body[shoulder].x+desired.x*(mirror?-1:1)*.28f,body[shoulder].y-desired.y*.28f,body[shoulder].z+desired.z*.28f);
                        body[wrist]=P(body[elbow].x,body[elbow].y+.25f,body[elbow].z);
                        yield return Send(udp,packet,ui.tracking.port,1.25f);
                        var rig=vrm.Instance.Runtime.ControlRig;
                        var arm=rig.GetBoneTransform(side==0?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
                        var end=rig.GetBoneTransform(side==0?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
                        float alignment=Vector3.Dot((end.position-arm.position).normalized,desired);
                        var handBone=rig.GetBoneTransform(side==0?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
                        var otherArm=rig.GetBoneTransform(side==0?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);
                        float scale=Vector3.Distance(arm.position,otherArm.position)/.6f;
                        var reach=new Vector3((body[wrist].x-body[shoulder].x)*(mirror?-1:1),-(body[wrist].y-body[shoulder].y),body[wrist].z-body[shoulder].z)*scale;
                        float maxReach=Vector3.Distance(arm.position,end.position)+Vector3.Distance(end.position,handBone.position);
                        float lengthA=Vector3.Distance(arm.position,end.position),lengthB=Vector3.Distance(end.position,handBone.position);
                        float minReach=Mathf.Sqrt(lengthA*lengthA+lengthB*lengthB+2*lengthA*lengthB*Mathf.Cos(150*Mathf.Deg2Rad));
                        if(reach.magnitude<minReach)reach=desired*minReach;
                        reach=reach.normalized*Mathf.Clamp(reach.magnitude,minReach,maxReach-.0001f);
                        Check(Vector3.Distance(handBone.position-arm.position,reach)<.08f,$"Arm wrist reach {direction}, side {side}, mirror {mirror}, error {Vector3.Distance(handBone.position-arm.position,reach):F3}");
                    }
                }
                foreach(bool mirror in new[]{false,true}) {
                    ui.tracking.mirror=mirror;
                    for(int side=0;side<2;side++)for(int direction=0;direction<4;direction++) {
                        int source=mirror?1-side:side,hip=source==0?23:24,knee=source==0?25:26,ankle=source==0?27:28;
                        Vector3 desired=direction==0?Vector3.down:direction==1?Vector3.back:direction==2?Vector3.forward:new Vector3(side==0?1:-1,-1,0).normalized;
                        body[knee]=P(body[hip].x+desired.x*(mirror?-1:1)*.34f,body[hip].y-desired.y*.34f,body[hip].z+desired.z*.34f);
                        body[ankle]=P(body[knee].x,body[knee].y+.34f,body[knee].z);
                        yield return Send(udp,packet,ui.tracking.port,1.25f);
                        var rig=vrm.Instance.Runtime.ControlRig;
                        var leg=rig.GetBoneTransform(side==0?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg);
                        var end=rig.GetBoneTransform(side==0?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg);
                        Check(Vector3.Dot((end.position-leg.position).normalized,desired)>.85f,$"Leg direction {direction}, side {side}, mirror {mirror}");
                    }
                    var hand=new Point[21];hand[0]=P(0,0);
                    for(int finger=0;finger<5;finger++)for(int joint=0;joint<4;joint++)hand[1+finger*4+joint]=P((finger-2)*.018f,-.03f-Mathf.Sin(joint*.6f)*.022f,-(1-Mathf.Cos(joint*.6f))*.03f);
                    packet.leftHand=hand;packet.rightHand=hand;packet.pose=false;
                    yield return Send(udp,packet,ui.tracking.port,1.25f);
                    foreach(var first in new[]{HumanBodyBones.LeftThumbProximal,HumanBodyBones.LeftIndexProximal,HumanBodyBones.LeftMiddleProximal,HumanBodyBones.LeftRingProximal,HumanBodyBones.LeftLittleProximal,HumanBodyBones.RightThumbProximal,HumanBodyBones.RightIndexProximal,HumanBodyBones.RightMiddleProximal,HumanBodyBones.RightRingProximal,HumanBodyBones.RightLittleProximal})for(int joint=0;joint<3;joint++) {
                        var key=(HumanBodyBones)((int)first+joint);var bone=vrm.Instance.Runtime.ControlRig.GetBoneTransform(key);
                        Check(bone!=null && Quaternion.Angle(bone.localRotation,Quaternion.identity)>5,$"Finger joint {key}, mirror {mirror}");
                    }
                    packet.pose=true;
                }
                packet.pose=false;
                foreach(bool mirror in new[]{false,true}) {
                    ui.tracking.mirror=mirror;packet.rightHand=null;
                    yield return Send(udp,packet,ui.tracking.port,.4f);
                    Check((ui.tracking.GetHand(true)!=null)==!mirror && (ui.tracking.GetHand(false)!=null)==mirror,"Anatomical hand assignment, mirror "+mirror);
                    packet.shapes=new[]{new Shape{name="eyeBlinkLeft",value=.9f}};
                    yield return Send(udp,packet,ui.tracking.port,.4f);
                    Check(expression.GetWeight(ExpressionKey.CreateCustom(mirror?"eyeBlinkRight":"eyeBlinkLeft"))>.8f && expression.GetWeight(ExpressionKey.CreateCustom(mirror?"eyeBlinkLeft":"eyeBlinkRight"))<.1f,"Independent blink assignment, mirror "+mirror);
                    float angle=20*Mathf.Deg2Rad;
                    packet.rotation=new[]{Mathf.Cos(angle),0,Mathf.Sin(angle),0,1f,0,-Mathf.Sin(angle),0,Mathf.Cos(angle)};
                    yield return Send(udp,packet,ui.tracking.port,.5f);
                    Check((ui.tracking.HeadRotation*Vector3.forward).x*(mirror?-1:1)>.2f,"Head yaw direction, mirror "+mirror);
                }
                ui.tracking.Calibrate();
                yield return Send(udp,packet,ui.tracking.port,.5f);
                Check(Quaternion.Angle(ui.tracking.HeadRotation,Quaternion.identity)<5,"Neutral head calibration");
                ui.tracking.mirror=false;packet.pose=false;
                int count=0;
                foreach(var key in expression.ExpressionKeys) {
                    if(key.Preset!=ExpressionPreset.custom || key.Name.StartsWith("viseme_"))continue;
                    packet.shapes=new[]{new Shape{name=key.Name,value=.9f}};
                    yield return Send(udp,packet,ui.tracking.port,.25f);
                    Check(expression.GetWeight(key)>.65f,"Facial channel "+key.Name);count++;
                }
                Check(count==52,"All 52 ARKit controls driven independently");
            }
            yield return new WaitForSecondsRealtime(2);
            Check(expression.GetWeight(jaw)<.05f,"Tracking loss restores neutral expression");
            Check(ui.tracking.port==15059,"Isolated synthetic tracker port");
            var saved=JsonUtility.FromJson<AvatarProfile>(JsonUtility.ToJson(ui.avatar.Profile));saved.Sanitize();
            Check(saved.style==1 && saved.hairStyle==0,"Identity profile roundtrip");
            string photo=Path.GetFullPath(Path.Combine(root,"..","..","Validation","reference.png"));
            if(File.Exists(photo)) {
                yield return ui.StartCoroutine((IEnumerator)ui.GetType().GetMethod("ImportPhoto",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(ui,new object[]{photo}));
                yield return ui.StartCoroutine((IEnumerator)ui.GetType().GetMethod("GenerateRoutine",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(ui,null));
                Check(ui.Vrm.Active && ui.Vrm.Appearance.IsCorporateHuman && ui.avatar.Profile.source=="photo","Photo reference customizes the actual MakeHuman model");
            }
            File.WriteAllText(Path.Combine(root,"corporate-result.txt"),failed?"FAIL":"PASS");
            Debug.Log("CORPORATE_RESULT="+(!failed));Application.Quit(failed?1:0);
        }
        static Point P(float x,float y,float z=0)=>new Point{x=x,y=y,z=z,confidence=1};
        static IEnumerator Send(UdpClient udp,TrackingFrame packet,int port,float duration){byte[] bytes=System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(packet));float start=Time.realtimeSinceStartup;while(Time.realtimeSinceStartup-start<duration){udp.Send(bytes,bytes.Length,"127.0.0.1",port);yield return null;}}
        static bool Enabled(VrmAvatarController vrm,string name){foreach(var r in vrm.Instance.GetComponentsInChildren<Renderer>())if(r.name.ToLowerInvariant().Contains(name))return r.enabled;return false;}
        void OnDisable(){Application.logMessageReceived-=Log;}
    }
}
