using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using UniVRM10;
using UnityEngine;
using UnityEngine.UI;

namespace WebcamAvatar
{
    [DefaultExecutionOrder(13000)]
    public sealed class CorporateSmoke : MonoBehaviour
    {
        [Serializable] class PartnerContent {public string answer;}
        bool failed;
        GestureAudit activeAudit;
        StudioUI auditUi;
        void LateUpdate(){if(activeAudit!=null&&auditUi!=null&&auditUi.Vrm.Active)activeAudit.Sample(auditUi);}
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
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--rig-parity")>=0){
                foreach(int mode in new[]{0,1}){
                    Call(ui,"SelectAvatarMode",mode);while(ui.Vrm.Loading)yield return null;
                    yield return VerifyRig(ui,(mode==0?"Cartoon":"Corporate")+" Webcam");
                    Call(ui,"ShowPartner");Check(!ui.tracking.Connected&&!ui.tracking.PoseDetected&&!ui.tracking.HandsDetected,"Partner clears prior webcam input model "+mode);
                    yield return VerifyRig(ui,(mode==0?"Cartoon":"Corporate")+" Partner");Call(ui,"ShowPage",3);
                }
                Debug.Log("CORPORATE_RIG_PARITY_RESULT="+(!failed));Application.Quit(failed?1:0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--partner-only")>=0){
                foreach(int mode in new[]{0,1}){
                    Call(ui,"SelectAvatarMode",mode);while(ui.Vrm.Loading)yield return null;
                    Call(ui,"ShowPartner");
                    var question=(InputField)ui.GetType().GetField("partnerQuestion",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(ui);question.text="Apresente-se como Dingous em uma única frase curta.";
                    Call(ui,"RunPartner","--question");float deadline=Time.realtimeSinceStartup+100;
                    var jobField=ui.GetType().GetField("partnerJob",BindingFlags.NonPublic|BindingFlags.Instance);
                    while(jobField.GetValue(ui)!=null&&Time.realtimeSinceStartup<deadline)yield return null;
                    var response=(InputField)ui.GetType().GetField("partnerAnswer",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(ui);
                    Check((bool)ui.GetType().GetField("partnerSucceeded",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(ui)&&jobField.GetValue(ui)==null&&response.text!="Dingous está pensando…"&&response.text.Length>10,"Partner answer and speech process completed model "+mode);
                    string replyPath=(string)ui.GetType().GetField("partnerLast",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(ui);
                    Check(response.text==JsonUtility.FromJson<PartnerContent>(File.ReadAllText(replyPath)).answer,"Full partner response displayed model "+mode);
                    yield return ui.StartCoroutine(ui.CaptureInterface(Path.Combine(Application.temporaryCachePath,"dingous-partner-"+mode+".png")));
                    Call(ui,"StopPartner");Call(ui,"ShowPage",3);
                }
                Debug.Log("CORPORATE_PARTNER_RESULT="+(!failed));Application.Quit(failed?1:0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--lab-only")>=0){
                foreach(int mode in new[]{0,1}){
                    Call(ui,"SelectAvatarMode",mode);while(ui.Vrm.Loading)yield return null;ui.avatar.previewAnimation=false;
                    var rig=ui.Vrm.Instance.Runtime.ControlRig;var wrist=rig.GetBoneTransform(HumanBodyBones.LeftHand);var initial=wrist.position;float movement=0,smile=0;bool packets=false;
                    string planPath=Path.Combine(Application.temporaryCachePath,"dingous-demo-"+mode+".json");
                    var labArgs=new List<string>{"--port",ui.tracking.port.ToString(),"--output",planPath};
                    if(Array.IndexOf(Environment.GetCommandLineArgs(),"--ai-lab")>=0){labArgs.Add("--plan");labArgs.Add(Path.GetFullPath("Validation/dingous-ai-plan.json"));}else labArgs.Add("--demo");
                    using(var labJob=new LocalJob("dingous_lab.py",labArgs.ToArray())){
                        float deadline=Time.realtimeSinceStartup+35;
                        while(!labJob.Finished&&Time.realtimeSinceStartup<deadline){packets|=ui.tracking.Connected;movement=Mathf.Max(movement,Vector3.Distance(initial,wrist.position));smile=Mathf.Max(smile,ui.tracking.GetExpression("mouthSmileLeft"));yield return null;}
                        Check(labJob.Finished&&labJob.ExitCode==0,"Dingous programmed demo process model "+mode);
                    }
                    Check(packets&&movement>.15f&&smile>.5f,"Dingous expressions and arm motion reach actual rig model "+mode);
                    Check(File.Exists(planPath),"Dingous reproducible plan saved model "+mode);
                }
                Call(ui,"ShowLaboratory");
                yield return ui.StartCoroutine(ui.CaptureInterface(Path.Combine(Application.temporaryCachePath,"dingous-laboratory.png")));
                Debug.Log("CORPORATE_LAB_RESULT="+(!failed));Application.Quit(failed?1:0);yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--face-only")>=0){
                using(var faceUdp=new UdpClient())foreach(int mode in new[]{0,1}){
                    Call(ui,"SelectAvatarMode",mode);while(ui.Vrm.Loading)yield return null;yield return null;
                    ui.avatar.previewAnimation=false;
                    foreach(bool mirror in new[]{false,true})foreach(bool left in new[]{true,false}){
                        ui.tracking.mirror=mirror;
                        var clear=new TrackingFrame{face=true,pose=false,shapes=Array.Empty<Shape>(),points=Array.Empty<Point>(),rotation=Array.Empty<float>()};
                        yield return Send(faceUdp,clear,ui.tracking.port,.3f);
                        clear.shapes=new[]{new Shape{name=left?"eyeBlinkLeft":"eyeBlinkRight",value=.7f},new Shape{name="mouthSmileLeft",value=.8f},new Shape{name="mouthSmileRight",value=.8f}};
                        yield return Send(faceUdp,clear,ui.tracking.port,.08f);
                        bool outputLeft=mirror?!left:left;var exp=ui.Vrm.Instance.Runtime.Expression;
                        float closed=0,open=0;
                        foreach(var key in exp.ExpressionKeys){
                            if(key.Name==(outputLeft?"eyeBlinkLeft":"eyeBlinkRight")||key.Preset==(outputLeft?ExpressionPreset.blinkLeft:ExpressionPreset.blinkRight))closed=Mathf.Max(closed,exp.ActualWeights.TryGetValue(key,out var weight)?weight:0);
                            if(key.Name==(outputLeft?"eyeBlinkRight":"eyeBlinkLeft")||key.Preset==(outputLeft?ExpressionPreset.blinkRight:ExpressionPreset.blinkLeft))open=Mathf.Max(open,exp.ActualWeights.TryGetValue(key,out var weight)?weight:0);
                        }
                        Check(closed>.9f&&open<.1f,$"80ms unilateral blink model {mode} left {left} mirror {mirror}");
                    }
                }
                Debug.Log("CORPORATE_FACE_RESULT="+(!failed));Application.Quit(failed?1:0);yield break;
            }
            var packetCounterAudit=new GestureAudit();packetCounterAudit.Sample(ui);packetCounterAudit.Sample(ui);
            Check(packetCounterAudit.samples==1,"Gesture audit counts each tracking packet once");
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
            var confidenceFilter=new AdaptivePoseFilter(1);
            confidenceFilter.Process(new[]{P(.5f,0)},0);
            var hidden=new[]{new Point{x=9,confidence=0}};
            var held=confidenceFilter.Process(hidden,.05f);
            Check(held[0].confidence>=.55f&&Mathf.Abs(held[0].x-.5f)<.001f,"Brief individual landmark occlusion preserves filtered pose");
            Check(confidenceFilter.Process(hidden,.2f)[0].confidence<.55f,"Expired individual landmark occlusion releases pose");
            foreach(var target in new[]{new Vector3(.15f,.45f,-.1f),new Vector3(-.15f,.45f,-.1f),new Vector3(0,-.4f,-.25f)}) {
                VrmAvatarController.SolveArm(target,new Vector3(.3f,.1f,0),.32f,.28f,out var upper,out var lower);
                Check(Vector3.Distance(upper*.32f+lower*.28f,target)<.001f,"IK reaches seated/head target "+target);
                Check(Vector3.Angle(upper,lower)<=150.01f,"IK elbow limit "+target);
            }
            if(!ui.Vrm.Active){Application.Quit(1);yield break;}
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","Validation"));Directory.CreateDirectory(root);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--corporate-live-e2e")>=0) {
                if(Array.IndexOf(Environment.GetCommandLineArgs(),"--cartoon")>=0){Call(ui,"SelectAvatarMode",0);while(ui.Vrm.Loading)yield return null;yield return null;}
                bool gestures=Array.IndexOf(Environment.GetCommandLineArgs(),"--gestures")>=0;
                bool faceGestures=Array.IndexOf(Environment.GetCommandLineArgs(),"--face-gestures")>=0;
                int leftWinks=0,rightWinks=0,reopened=0;
                var audit=new GestureAudit();
                if(Array.IndexOf(Environment.GetCommandLineArgs(),"--preview")>=0)Call(ui,"SetTrackingPreview",true);
                Call(ui,"ToggleTracking");Call(ui,"ToggleCamera");
                float connectedAt=-1,captureStart=-1;bool cameraReady=false,clean=false,faceSeen=false,poseSeen=false,handsSeen=false;start=Time.realtimeSinceStartup;
                while(gestures?(captureStart<0?Time.realtimeSinceStartup-start<45:Time.realtimeSinceStartup-captureStart<75):Time.realtimeSinceStartup-start<30) {
                    if(ui.tracking.Connected && connectedAt<0){connectedAt=Time.realtimeSinceStartup;Debug.Log("CORPORATE_LIVE_TRACKER_READY");}
                    if(!cameraReady && ui.GetComponent<VirtualCameraOutput>().Sending){cameraReady=true;Debug.Log("CORPORATE_LIVE_CAMERA_READY");}
                    faceSeen|=ui.tracking.FaceDetected;poseSeen|=ui.tracking.PoseDetected;handsSeen|=ui.tracking.HandsDetected;
                    if(gestures&&captureStart<0&&cameraReady&&ui.tracking.Connected){captureStart=Time.realtimeSinceStartup;activeAudit=audit;auditUi=ui;Debug.Log("CORPORATE_GESTURES_READY");}
                    if(faceGestures&&captureStart>=0){
                        float l=ui.tracking.GetExpression("eyeBlinkLeft"),r=ui.tracking.GetExpression("eyeBlinkRight"),ml=0,mr=0;
                        var ex=ui.Vrm.Instance.Runtime.Expression;
                        foreach(var key in ex.ExpressionKeys){
                            if(key.Name=="eyeBlinkLeft"||key.Preset==ExpressionPreset.blinkLeft)ml=Mathf.Max(ml,ex.ActualWeights.TryGetValue(key,out var actual)?actual:0);
                            if(key.Name=="eyeBlinkRight"||key.Preset==ExpressionPreset.blinkRight)mr=Mathf.Max(mr,ex.ActualWeights.TryGetValue(key,out var actual)?actual:0);
                        }
                        if(l>.7f&&r<.25f&&ml>.85f&&mr<.25f)leftWinks++;
                        if(r>.7f&&l<.25f&&mr>.85f&&ml<.25f)rightWinks++;
                        if(l<.15f&&r<.15f&&ml<.15f&&mr<.15f)reopened++;
                    }
                    if(!gestures&&!clean && Time.realtimeSinceStartup-start>15){clean=true;ui.avatar.previewAnimation=true;ui.avatar.previewExpressionPreset=4;Call(ui,"SetPresentation",true);}
                    yield return null;
                }
                activeAudit=null;
                Check(connectedAt>=0,"Physical webcam produced valid tracking packets");
                if(gestures){audit.Save(Path.Combine(root,"corporate-gesture-audit.json"));if(!faceGestures)Check(audit.Passed,"Real webcam head reach, arms and finger curl on both sides");}
                if(faceGestures){Debug.Log($"CORPORATE_WINK_SAMPLES left={leftWinks} right={rightWinks} reopened={reopened}");Check(leftWinks>5&&rightWinks>5&&reopened>5,"Real unilateral winks and eyelid reopening");}
                Debug.Log($"CORPORATE_LIVE_DETECTIONS face={faceSeen} pose={poseSeen} hands={handsSeen}");
                var broadcaster=ui.GetComponent<VirtualCameraOutput>();
                Check(broadcaster.Sending && broadcaster.Error==null,"Unity camera connected to OBS bridge on TCP 5060");
                Call(ui,"ToggleTracking");Call(ui,"StopCamera");
                File.WriteAllText(Path.Combine(root,"corporate-live-result.txt"),failed?"FAIL":"PASS");
                Debug.Log("CORPORATE_LIVE_RESULT="+(!failed));Application.Quit(failed?1:0);yield break;
            }
            var vrm=ui.Vrm;var appearance=vrm.Appearance;
            Call(ui,"SelectAvatarMode",0);while(vrm.Loading)yield return null;yield return null;
            Check(vrm.Active&&!ui.avatar.Profile.corporate&&vrm.ModelName.Contains("Seed"),"Cartoon selector loads actual Seed-san VRM");
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--decorations-only")<0)yield return VerifyRig(ui,"Cartoon");
            Call(ui,"SelectAvatarMode",1);while(vrm.Loading)yield return null;yield return null;
            appearance=vrm.Appearance;
            Check(vrm.Active&&ui.avatar.Profile.corporate&&appearance.IsCorporateHuman,"Corporate selector restores actual MakeHuman");
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--decorations-only")<0)yield return VerifyRig(ui,"Corporativo");
            yield return new WaitForSecondsRealtime(.5f);
            Check(appearance!=null && appearance.IsCorporateHuman,"Actual MakeHuman identity morphs available");
            Check(vrm.ExpressionCount>=67,"52 ARKit expressions and 15 visemes");
            Call(ui,"ShowPage",1);Call(ui,"FrameVrm",true);
            for(int outfit=0;outfit<=6;outfit++){
                ui.avatar.Profile.clothingStyle=outfit;vrm.ApplyAppearance();Call(ui,"SyncAppearance");
                for(int garment=0;garment<=6;garment++)Check(Enabled(vrm,garment==0?"male_elegantsuit01":"male_casualsuit"+garment.ToString("00"))==(outfit==garment),"Exclusive garment "+garment+" for outfit "+outfit);
                var wardrobeProfile=JsonUtility.FromJson<AvatarProfile>(JsonUtility.ToJson(ui.avatar.Profile));wardrobeProfile.Sanitize();Check(wardrobeProfile.clothingStyle==outfit,"Clothing selection profile roundtrip "+outfit);
                yield return ui.StartCoroutine(ui.CaptureInterface(Path.Combine(root,"wardrobe-"+outfit+".png")));
            }
            var logoTexture=new Texture2D(64,32,TextureFormat.RGBA32,false);
            var logoPixels=new Color[64*32];for(int i=0;i<logoPixels.Length;i++)logoPixels[i]=(i%64>8&&i%64<56)?Color.red:Color.clear;
            logoTexture.SetPixels(logoPixels);logoTexture.Apply();string logoPath=Path.Combine(root,"shirt-logo-fixture.png");File.WriteAllBytes(logoPath,logoTexture.EncodeToPNG());Destroy(logoTexture);
            ui.avatar.Profile.clothingStyle=6;ui.avatar.Profile.shirtLogoPath=logoPath;
            for(int pattern=0;pattern<4;pattern++){
                ui.avatar.Profile.shirtPattern=pattern;vrm.ApplyAppearance();
                bool printed=false;foreach(var mesh in vrm.Instance.GetComponentsInChildren<SkinnedMeshRenderer>())if(mesh.enabled&&mesh.name.ToLowerInvariant().Contains("male_casualsuit06"))foreach(var material in mesh.sharedMaterials)printed|=material.shader.name=="CorporateAvatar/ShirtPrint"&&material.GetFloat("_Pattern")==pattern&&material.GetFloat("_HasLogo")==1&&material.GetTexture("_Logo")!=null;
                Check(printed,"Shirt pattern and uploaded logo bound "+pattern);
            }
            Call(ui,"FrameVrm",false);Call(ui,"ShowAdvanced");
            yield return ui.StartCoroutine(ui.CaptureInterface(Path.Combine(root,"shirt-logo-customization.png")));
            Call(ui,"ShowPage",2);
            ui.avatar.Profile.backgroundPath=logoPath;Call(ui,"SetEnvironment",5);
            Check(ui.avatar.Profile.environment==5,"Uploaded background selected");
            var decorationProfile=JsonUtility.FromJson<AvatarProfile>(JsonUtility.ToJson(ui.avatar.Profile));decorationProfile.Sanitize();
            Check(decorationProfile.shirtPattern==3&&decorationProfile.backgroundPath==logoPath&&decorationProfile.shirtLogoPath==logoPath,"Decoration profile roundtrip");
            ui.avatar.Profile.shirtLogoPath=null;ui.avatar.Profile.shirtPattern=0;Call(ui,"SetEnvironment",0);
            ui.avatar.Profile.clothingStyle=0;vrm.ApplyAppearance();Call(ui,"SyncAppearance");
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--decorations-only")>=0){Debug.Log("CORPORATE_DECORATIONS_RESULT="+(!failed));Application.Quit(failed?1:0);yield break;}
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
        IEnumerator VerifyRig(StudioUI ui,string label)
        {
            var rig=ui.Vrm.Instance.Runtime.ControlRig;var packet=new TrackingFrame{pose=true,face=false,shapes=Array.Empty<Shape>(),rotation=Array.Empty<float>()};
            var attachments=new Dictionary<Transform,Vector3>();
            foreach(HumanBodyBones key in Enum.GetValues(typeof(HumanBodyBones))){var bone=rig.GetBoneTransform(key);if(bone!=null)attachments[bone]=bone.localPosition;}
            using(var udp=new UdpClient())foreach(bool mirror in new[]{false,true}){
                ui.tracking.mirror=mirror;
                var points=new Point[33];for(int i=0;i<33;i++)points[i]=new Point();
                points[0]=P(0,-.8f);points[11]=P(.2f,-.5f);points[12]=P(-.2f,-.5f);
                // Elbows deliberately occluded: desk cameras often lose them.
                points[15]=P(.02f,-.8f);points[16]=P(-.02f,-.8f);packet.points=points;
                yield return Send(udp,packet,ui.tracking.port,1.4f);
                var head=rig.GetBoneTransform(HumanBodyBones.Head);
                foreach(var side in new[]{HumanBodyBones.LeftHand,HumanBodyBones.RightHand})Check(Vector3.Distance(rig.GetBoneTransform(side).position,head.position)<.3f,label+" head reach with hidden elbow "+side+" mirror "+mirror);
                for(int side=0;side<2;side++){
                    int source=mirror?1-side:side,s=source==0?11:12,e=source==0?13:14,w=source==0?15:16;
                    var upper=rig.GetBoneTransform(side==0?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);var lower=rig.GetBoneTransform(side==0?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);var hand=rig.GetBoneTransform(side==0?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
                    foreach(float height in new[]{-.3f,.3f}){
                        points[e]=P(points[s].x,points[s].y-height*.5f,-.12f);points[w]=P(points[s].x,points[s].y-height,-.2f);
                        yield return Send(udp,packet,ui.tracking.port,1.3f);
                        Check((hand.position-upper.position).y*height>0,label+" arm raise/lower "+side+" "+height+" mirror "+mirror+" height "+(hand.position-upper.position).y+" target "+(ui.Vrm.WristTargets[side].y-upper.position.y)+" alignment "+ui.Vrm.ArmAlignment(side==0)+" immediate "+ui.Vrm.ImmediateAlignment[side]);
                        Check(Vector3.Angle(lower.position-upper.position,hand.position-lower.position)<=151,label+" elbow anatomy "+side+" mirror "+mirror);
                        Check(Vector3.Distance(hand.position,ui.Vrm.WristTargets[side])<.04f,label+" wrist endpoint within 4 cm "+side+" mirror "+mirror+" error "+Vector3.Distance(hand.position,ui.Vrm.WristTargets[side]));
                        Check(StableMesh(ui.Vrm),label+" skinned vertices finite and bounded "+side+" mirror "+mirror);
                    }
                }
                var trackedHand=new Point[21];trackedHand[0]=P(0,0);
                var first=new[]{HumanBodyBones.LeftThumbProximal,HumanBodyBones.LeftIndexProximal,HumanBodyBones.LeftMiddleProximal,HumanBodyBones.LeftRingProximal,HumanBodyBones.LeftLittleProximal,HumanBodyBones.RightThumbProximal,HumanBodyBones.RightIndexProximal,HumanBodyBones.RightMiddleProximal,HumanBodyBones.RightRingProximal,HumanBodyBones.RightLittleProximal};
                var open=new Quaternion[30];
                for(int curl=0;curl<2;curl++){
                    for(int f=0;f<5;f++)for(int j=0;j<4;j++)trackedHand[1+f*4+j]=P((f-2)*.018f,-.03f-(curl==0?j*.018f:Mathf.Sin(j*.6f)*.025f),curl==0?0:-(1-Mathf.Cos(j*.6f))*.04f);
                    packet.leftHand=trackedHand;packet.rightHand=trackedHand;
                    yield return Send(udp,packet,ui.tracking.port,1.4f);
                    int index=0;foreach(var start in first)for(int j=0;j<3;j++,index++){
                        var bone=rig.GetBoneTransform((HumanBodyBones)((int)start+j));
                        if(curl==0)open[index]=bone.localRotation;else Check(Quaternion.Angle(open[index],bone.localRotation)>3,label+" finger opens/closes "+start+" joint "+j+" mirror "+mirror);
                    }
                }
                packet.face=true;packet.rotation=new[]{1f,0,0,0,1f,0,0,0,1f};
                packet.shapes=new[]{new Shape{name="jawOpen",value=.9f},new Shape{name="eyeBlinkLeft",value=.9f}};
                yield return Send(udp,packet,ui.tracking.port,1);
                var expressions=ui.Vrm.Instance.Runtime.Expression;
                bool customFace=false;foreach(var key in expressions.ExpressionKeys)if(key.Preset==ExpressionPreset.custom&&key.Name=="jawOpen")customFace=true;
                var mouth=customFace?ExpressionKey.CreateCustom("jawOpen"):ExpressionKey.Aa;
                var eye=customFace?ExpressionKey.CreateCustom(mirror?"eyeBlinkRight":"eyeBlinkLeft"):ExpressionKey.CreateFromPreset(mirror?ExpressionPreset.blinkRight:ExpressionPreset.blinkLeft);
                Check(expressions.GetWeight(mouth)>.6f,label+" jaw expression mirror "+mirror);
                Check(expressions.GetWeight(eye)>.6f,label+" unilateral blink mirror "+mirror);
                var neutralHead=head.rotation;float yaw=.3f;
                packet.rotation=new[]{Mathf.Cos(yaw),0,Mathf.Sin(yaw),0,1,0,-Mathf.Sin(yaw),0,Mathf.Cos(yaw)};
                yield return Send(udp,packet,ui.tracking.port,1.1f);
                Check(Quaternion.Angle(neutralHead,head.rotation)>10,label+" head rotation mirror "+mirror);
                packet.face=false;packet.shapes=Array.Empty<Shape>();packet.leftHand=null;packet.rightHand=null;
                points[23]=P(.15f,0);points[24]=P(-.15f,0);
                for(int side=0;side<2;side++){
                    int source=mirror?1-side:side,h=source==0?23:24,k=source==0?25:26,a=source==0?27:28;
                    var hip=rig.GetBoneTransform(side==0?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg);var knee=rig.GetBoneTransform(side==0?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg);
                    foreach(float forward in new[]{-.25f,.25f}){
                        points[k]=P(points[h].x,.3f,forward);points[a]=P(points[h].x,.6f,forward);
                        yield return Send(udp,packet,ui.tracking.port,1.1f);
                        Check((knee.position-hip.position).z*forward>0,label+" leg forward/back "+side+" mirror "+mirror);
                    }
                }
                points[11].x+=.15f;points[12].x+=.15f;
                yield return Send(udp,packet,ui.tracking.port,1.1f);
                Check(Mathf.Abs((rig.GetBoneTransform(HumanBodyBones.Spine).rotation*Vector3.up).x)>.1f,label+" torso lean mirror "+mirror);
                bool attached=true;foreach(var pair in attachments)attached&=Vector3.Distance(pair.Key.localPosition,pair.Value)<.00001f;
                Check(attached,label+" joints retain their attachments mirror "+mirror);
            }
        }
        static bool StableMesh(VrmAvatarController avatar)
        {
            var hip=avatar.Instance.Runtime.ControlRig.GetBoneTransform(HumanBodyBones.Hips).position;
            foreach(var renderer in avatar.Instance.GetComponentsInChildren<SkinnedMeshRenderer>()){
                if(!renderer.enabled||!renderer.gameObject.activeInHierarchy)continue;
                var mesh=new Mesh();renderer.BakeMesh(mesh);bool valid=true;
                foreach(var vertex in mesh.vertices){var p=renderer.transform.TransformPoint(vertex);if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z)||Vector3.Distance(p,hip)>3){valid=false;break;}}
                Destroy(mesh);if(!valid)return false;
            }
            return true;
        }
        static IEnumerator Send(UdpClient udp,TrackingFrame packet,int port,float duration){byte[] bytes=System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(packet));float start=Time.realtimeSinceStartup;while(Time.realtimeSinceStartup-start<duration){udp.Send(bytes,bytes.Length,"127.0.0.1",port);yield return null;}}
        static bool Enabled(VrmAvatarController vrm,string name){foreach(var r in vrm.Instance.GetComponentsInChildren<Renderer>())if(r.name.ToLowerInvariant().Contains(name))return r.enabled;return false;}
        void OnDisable(){Application.logMessageReceived-=Log;}
    }
}
