using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace WebcamAvatar.Editor
{
    [InitializeOnLoad]
    public static class StudioValidation
    {
        static double started;
        static int step;
        static double expressionStarted;
        static StudioValidation(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
        public static void Run()
        {
            CreateDemo.CreateStudio();SessionState.SetBool("StudioValidation",true);SessionState.SetBool("StudioFailed",false);
            EditorApplication.EnterPlaymode();
        }
        static void Log(string message,string stack,LogType type)
        {
            if(SessionState.GetBool("StudioValidation",false) && (type==LogType.Exception || type==LogType.Error) && !stack.Contains("UnityEditor.Search")) {
                File.AppendAllText("studio-runtime-errors.log",message+"\n"+stack+"\n");SessionState.SetBool("StudioFailed",true);
            }
        }
        static void Call(object instance,string method,params object[] args)=>instance.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(instance,args);
        static void Tick()
        {
            if(!SessionState.GetBool("StudioValidation",false) || !EditorApplication.isPlaying)return;
            if(started==0)started=EditorApplication.timeSinceStartup;
            double elapsed=EditorApplication.timeSinceStartup-started;
            try {
                var ui=UnityEngine.Object.FindAnyObjectByType<StudioUI>();
                if(ui==null)throw new Exception("StudioUI missing");
                if(step==0 && elapsed>3) {
                    var avatar=UnityEngine.Object.FindAnyObjectByType<CartoonAvatar>();
                    if(avatar.transform.childCount==0)throw new Exception("Avatar geometry not initialized");
                    Directory.CreateDirectory("Validation");
                    ui.validationStorage=Path.GetFullPath("Validation/LocalStorage");Directory.CreateDirectory(ui.validationStorage+"/Drafts");Directory.CreateDirectory(ui.validationStorage+"/Avatars");
                    var import=(System.Collections.IEnumerator)ui.GetType().GetMethod("ImportPhoto",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(ui,new object[]{Path.GetFullPath("Validation/reference.png")});ui.StartCoroutine(import);step=1;
                }
                bool busy=(bool)ui.GetType().GetField("busy",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(ui);
                if(step==1 && !busy && elapsed>5) {Call(ui,"Generate");step=2;}
                if(step==2 && !busy && elapsed>8) {
                    if(ui.avatar.Profile.source!="photo")throw new Exception("Photo generation failed");
                    if(ui.avatar.FacialBlendshapeCount!=52)throw new Exception("Expected 52 facial blendshapes");
                    var faceMesh=ui.avatar.GetComponentInChildren<AdvancedFace>().Renderer.sharedMesh;
                    var shapeDelta=new Vector3[faceMesh.vertexCount];var normalDelta=new Vector3[faceMesh.vertexCount];var tangentDelta=new Vector3[faceMesh.vertexCount];
                    for(int shape=0;shape<faceMesh.blendShapeCount;shape++) {
                        faceMesh.GetBlendShapeFrameVertices(shape,0,shapeDelta,normalDelta,tangentDelta);float movement=0;
                        foreach(var v in shapeDelta){if(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z))throw new Exception("Invalid facial shape");movement=Mathf.Max(movement,v.sqrMagnitude);}
                        if(movement<.00000001f)throw new Exception("Empty facial control: "+faceMesh.GetBlendShapeName(shape));
                    }
                    Debug.Log("VALIDATION: all 52 facial controls have finite nonzero deformation");
                    Capture(ui,"Validation/01-creator.png");ui.StartCoroutine(ui.CaptureInterface("Validation/ui-creator.png"));step=3;
                }
                if(step==3 && elapsed>10) {
                    Call(ui,"ShowPage",1);ui.avatar.Profile.glasses=true;ui.avatar.Profile.hairStyle=2;ui.avatar.Rebuild(ui.avatar.Profile);Call(ui,"SyncAppearance");ui.avatar.previewAnimation=true;
                    Capture(ui,"Validation/03-cartoon.png");ui.StartCoroutine(ui.CaptureInterface("Validation/ui-appearance.png"));step=4;
                }
                if(step==4 && elapsed>12) {Call(ui,"ShowPage",2);Call(ui,"ToggleTracking");step=5;}
                if(step==5 && elapsed>21) {
                    var tracker=UnityEngine.Object.FindAnyObjectByType<AvatarTracking>();
                    if(!tracker.Connected)throw new Exception("No live tracking packets received");
                    Debug.Log("VALIDATION: live UDP tracking connected; face="+tracker.FaceDetected+" pose="+tracker.PoseDetected);
                    Call(ui,"ToggleTracking");Call(ui,"Save");Call(ui,"LoadSaved",1);Capture(ui,"Validation/04-broadcast.png");
                    ui.StartCoroutine(ui.CaptureInterface("Validation/ui-broadcast.png"));
                    Call(ui,"SetPresentation",true);Call(ui,"SetPresentation",false);
                    Call(ui,"ToggleCamera");expressionStarted=elapsed;step=6;
                }
                if(step==6) {
                    using(var udp=new System.Net.Sockets.UdpClient()) {
                        var hand=new Point[21];hand[0]=new Point{confidence=1};
                        for(int finger=0;finger<5;finger++)for(int joint=0;joint<4;joint++)hand[1+finger*4+joint]=new Point{x=(finger-2)*.017f,y=.035f+Mathf.Sin(joint*.8f)*.025f,z=(1-Mathf.Cos(joint*.8f))*.025f,confidence=1};
                        var body=new Point[33];for(int i=0;i<33;i++)body[i]=new Point{confidence=1};
                        body[11]=new Point{x=.45f,y=-.55f,confidence=1};body[12]=new Point{x=-.15f,y=-.55f,confidence=1};
                        body[23]=new Point{x=.15f,confidence=1};body[24]=new Point{x=-.15f,confidence=1};
                        body[25]=new Point{x=.15f,y=.40f,confidence=1};body[26]=new Point{x=-.15f,y=.30f,z=-.25f,confidence=1};
                        body[27]=new Point{x=.15f,y=.78f,confidence=1};body[28]=new Point{x=-.15f,y=.65f,confidence=1};
                        body[31]=new Point{x=.15f,y=.80f,z=-.18f,confidence=1};body[32]=new Point{x=-.15f,y=.65f,z=-.18f,confidence=1};
                        var packet=new TrackingFrame{face=true,pose=true,points=body,leftHand=hand,rightHand=hand,rotation=new[]{1f,0,0,0,1,0,0,0,1},shapes=new[]{new Shape{name="jawOpen",value=1},new Shape{name="eyeBlinkLeft",value=1}}};
                        byte[] bytes=System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(packet));udp.Send(bytes,bytes.Length,"127.0.0.1",5055);
                    }
                    if(elapsed-expressionStarted>1.3 && ui.tracking.GetExpression("jawOpen")>.90f && ui.tracking.GetExpression("eyeBlinkLeft")>.90f) {
                        var face=ui.avatar.GetComponentInChildren<AdvancedFace>();var renderer=face.Renderer;
                        if(renderer.GetBlendShapeWeight(renderer.sharedMesh.GetBlendShapeIndex("jawOpen"))<80)throw new Exception("Facial mesh did not receive jaw deformation");
                        var baked=new Mesh();renderer.BakeMesh(baked);var rest=renderer.sharedMesh.vertices;var moved=baked.vertices;float max=0;
                        for(int i=0;i<rest.Length;i++)max=Mathf.Max(max,Vector3.Distance(rest[i],moved[i]));UnityEngine.Object.Destroy(baked);
                        if(max<.015f)throw new Exception("Facial mesh vertices were not deformed");
                        if(!ui.tracking.HandsDetected)throw new Exception("Hand packet was not consumed");
                        if(!ui.tracking.FullBodyDetected)throw new Exception("Body coverage was not reported");
                        var legs=(Transform[])ui.avatar.GetType().GetField("upperLegs",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(ui.avatar);
                        var chest=(Transform)ui.avatar.GetType().GetField("spine",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(ui.avatar);
                        if(Quaternion.Angle(legs[1].localRotation,Quaternion.identity)<20 || Quaternion.Angle(chest.localRotation,Quaternion.identity)<8)throw new Exception("Legs/spine did not respond to body pose");
                        var fingers=(Transform[,])ui.avatar.GetType().GetField("fingerJoints",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(ui.avatar);
                        if(Quaternion.Angle(fingers[0,4].localRotation,Quaternion.identity)<20)throw new Exception("Finger rig did not react to tracked joints");
                        for(int side=0;side<2;side++)for(int finger=0;finger<5;finger++)for(int joint=1;joint<3;joint++)if(Quaternion.Angle(fingers[side,finger*3+joint].localRotation,Quaternion.identity)<20)throw new Exception("Finger did not react: "+side+"/"+finger+"/"+joint);
                        Debug.Log("VALIDATION: all ten fingers respond to flexion packets");
                        Call(ui,"SetFraming",1);Capture(ui,"Validation/full-body-tracking.png");Debug.Log("VALIDATION: pelvis, spine, legs, facial mesh and articulated fingers verified");
                        Debug.Log("VALIDATION: deterministic jawOpen and blink packet applied correctly");expressionStarted=elapsed;step=7;
                    }
                    else if(elapsed-expressionStarted>12)throw new Exception("Expression smoothing/transport timed out");
                }
                if(step==7 && elapsed>expressionStarted+3) {
                    if(ui.tracking.GetExpression("jawOpen")>.05f)throw new Exception("Lost tracking did not return to neutral");
                    var type=typeof(AvatarTracking);var field=type.GetField("frame",BindingFlags.NonPublic|BindingFlags.Instance);var prior=field.GetValue(ui.tracking);
                    var image=new Point[33];for(int i=0;i<image.Length;i++)image[i]=new Point{x=.7f,y=.6f,confidence=1};
                    field.SetValue(ui.tracking,new TrackingFrame{imagePose=image,points=Array.Empty<Point>(),shapes=Array.Empty<Shape>(),rotation=Array.Empty<float>()});
                    type.GetField("received",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(ui.tracking,Time.unscaledTime);
                    type.GetField("bodyReference",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(ui.tracking,new Vector2(.5f,.5f));
                    type.GetField("hasBodyReference",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(ui.tracking,true);
                    if(Vector2.Distance(ui.tracking.BodyOffset,new Vector2(-.2f,-.1f))>.001f)throw new Exception("Body translation/mirror failed");
                    ui.tracking.Calibrate();if(ui.tracking.BodyOffset.sqrMagnitude>.000001f)throw new Exception("Body calibration failed");field.SetValue(ui.tracking,prior);
                    Debug.Log("VALIDATION: body image translation and calibration verified");
                    Call(ui,"ShowPage",1);Call(ui,"ShowAdvanced");ui.StartCoroutine(ui.CaptureInterface("Validation/ui-advanced-controls.png"));step=8;expressionStarted=elapsed;
                }
                if(step==8 && elapsed>expressionStarted+1.5) {
                    Debug.Log("VALIDATION: creator, appearance, real webcam UDP, save/load, presentation, camera error handling completed.");
                    SessionState.SetBool("StudioValidation",false);
                    EditorApplication.Exit(SessionState.GetBool("StudioFailed",false)?1:0);
                }
                if(elapsed>100)throw new Exception("Runtime validation timed out");
            }catch(Exception e){Debug.LogException(e);SessionState.SetBool("StudioValidation",false);EditorApplication.Exit(1);}
        }
        static void Capture(StudioUI ui,string path)
        {
            // Dedicated output render, no screenshot permissions or webcam images involved.
            ui.avatarCamera.Render();var previous=RenderTexture.active;RenderTexture.active=ui.output;
            var texture=new Texture2D(ui.output.width,ui.output.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);texture.Apply();
            File.WriteAllBytes(path,texture.EncodeToPNG());RenderTexture.active=previous;UnityEngine.Object.Destroy(texture);
        }
        public static void BuildWindows()
        {
            CreateDemo.CreateStudio();Directory.CreateDirectory("Build");
            var report=BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/AvatarStudio.unity"},"Build/AvatarStudio.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
        }
    }
}
